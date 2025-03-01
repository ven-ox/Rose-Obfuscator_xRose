﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using Microsoft.CSharp;
using PRose.Properties;
using Rose.Helper;

namespace Rose.Protections
{
	// Token: 0x0200004F RID: 79
	internal class Packer2
	{
		// Token: 0x060000FE RID: 254 RVA: 0x0000A0B4 File Offset: 0x000082B4
		private static void InjectEraseMethod(ModuleDef module)
		{
			IEnumerable<IDnlibDef> source;
			Packer2.eraseMethod = (MethodDef)source.Single((IDnlibDef method) => method.Name == "KOISZ");
			ModuleDefMD moduleDefMD = ModuleDefMD.Load(typeof(HeaderErasePacker).Module);
			TypeDef typeDef = moduleDefMD.ResolveTypeDef(MDToken.ToRID(typeof(HeaderErasePacker).MetadataToken));
			IEnumerator<MethodDef> enumerator = module.GlobalType.Methods.GetEnumerator();
			source = InjectHelper.Inject(typeDef, module.GlobalType, module);
			try
			{
				MethodDef methodDef;
				for (;;)
				{
					methodDef = enumerator.Current;
					if (methodDef.Name == ".ctor")
					{
						break;
					}
					if (!enumerator.MoveNext())
					{
						goto IL_E0;
					}
				}
				module.GlobalType.Remove(methodDef);
				IL_E0:;
			}
			finally
			{
				if (enumerator != null)
				{
					enumerator.Dispose();
				}
			}
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000A1C4 File Offset: 0x000083C4
		private static void InjectAntiDebugMethod(ModuleDef module)
		{
			ModuleDefMD moduleDefMD;
			TypeDef typeDef = moduleDefMD.ResolveTypeDef(MDToken.ToRID(typeof(DebugCheckerPacker).MetadataToken));
			moduleDefMD = ModuleDefMD.Load(typeof(DebugCheckerPacker).Module);
			IEnumerable<IDnlibDef> source = InjectHelper.Inject(typeDef, module.GlobalType, module);
			Packer2.debuggerMethod = (MethodDef)source.Single((IDnlibDef method) => method.Name == "XERPO");
			using (IEnumerator<MethodDef> enumerator = module.GlobalType.Methods.GetEnumerator())
			{
				MethodDef methodDef;
				for (;;)
				{
					methodDef = enumerator.Current;
					if (methodDef.Name == ".ctor")
					{
						break;
					}
					if (!enumerator.MoveNext())
					{
						goto IL_E0;
					}
				}
				module.GlobalType.Remove(methodDef);
				IL_E0:;
			}
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000A2D4 File Offset: 0x000084D4
		public static byte[] Pack(string outputPath, string inputPath, string encryptKey, bool erasePE, bool antiDebug)
		{
			byte[] array;
			string text;
			if (erasePE || antiDebug)
			{
				ModuleDefMD moduleDefMD;
				IEnumerator<TypeDef> enumerator;
				do
				{
					if (antiDebug)
					{
						array = File.ReadAllBytes(inputPath);
					}
					enumerator = moduleDefMD.GetTypes().GetEnumerator();
					Packer2.InjectAntiDebugMethod(moduleDefMD);
					moduleDefMD = ModuleDefMD.Load(array);
					Packer2.InjectEraseMethod(moduleDefMD);
					if (File.Exists(text))
					{
						break;
					}
				}
				while (!erasePE);
				text = Path.GetTempPath() + Guid.NewGuid().ToString() + ".exe";
				try
				{
					do
					{
						TypeDef typeDef = enumerator.Current;
						if (!typeDef.IsGlobalModuleType)
						{
							using (IEnumerator<MethodDef> enumerator2 = typeDef.Methods.GetEnumerator())
							{
								while (enumerator2.MoveNext())
								{
									MethodDef methodDef = enumerator2.Current;
									if (methodDef.HasBody && methodDef.Name.Contains("Main"))
									{
										if (antiDebug)
										{
											methodDef.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Packer2.debuggerMethod));
										}
										if (erasePE)
										{
											methodDef.Body.Instructions.Insert(methodDef.Body.Instructions.Count - 2, Instruction.Create(OpCodes.Call, Packer2.eraseMethod));
										}
									}
								}
								goto IL_9C;
							}
							continue;
						}
						IL_9C:;
					}
					while (enumerator.MoveNext());
				}
				finally
				{
					if (enumerator != null)
					{
						enumerator.Dispose();
					}
				}
				moduleDefMD.Write(text, new ModuleWriterOptions(moduleDefMD)
				{
					Logger = DummyLogger.NoThrowInstance
				});
			}
			byte[] salt = new byte[] { 5, 10, 15, 20, 25, 30, 35, 40 };
			byte[] array2;
			if (erasePE)
			{
				array2 = File.ReadAllBytes(text);
			}
			else
			{
				array2 = array;
			}
			MemoryStream memoryStream = new MemoryStream();
			RijndaelManaged rijndaelManaged = new RijndaelManaged();
			rijndaelManaged.KeySize = 128;
			rijndaelManaged.BlockSize = 128;
			Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(encryptKey, salt, 1000);
			rijndaelManaged.Key = rfc2898DeriveBytes.GetBytes(rijndaelManaged.KeySize / 8);
			rijndaelManaged.IV = rfc2898DeriveBytes.GetBytes(rijndaelManaged.BlockSize / 8);
			rijndaelManaged.Mode = CipherMode.CBC;
			CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateEncryptor(), CryptoStreamMode.Write);
			cryptoStream.Write(array2, 0, array2.Length);
			cryptoStream.Close();
			array2 = memoryStream.ToArray();
			string newValue = Convert.ToBase64String(memoryStream.ToArray());
			string text2 = Resources.XROSE;
			text2 = text2.Replace("%ASSEMBLY%", newValue);
			text2 = text2.Replace("%KEY%", encryptKey);
			Assembly assembly = Assembly.Load(array);
			CSharpCodeProvider csharpCodeProvider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
			CompilerParameters compilerParameters = new CompilerParameters();
			compilerParameters.CompilerOptions = "/target:winexe";
			compilerParameters.GenerateExecutable = true;
			AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
			AssemblyName[] array3 = referencedAssemblies;
			for (;;)
			{
				int num;
				AssemblyName assemblyName = array3[num];
				if (assemblyName.Name.Contains("System."))
				{
					goto IL_3B7;
				}
				if (assemblyName.Name.Contains("Microsoft."))
				{
					goto IL_3B7;
				}
				IL_3D9:
				num++;
				if (num >= array3.Length)
				{
					break;
				}
				continue;
				IL_3B7:
				compilerParameters.ReferencedAssemblies.Add(assemblyName.Name + ".dll");
				goto IL_3D9;
			}
			CompilerResults compilerResults = csharpCodeProvider.CompileAssemblyFromSource(compilerParameters, new string[] { text2 });
			FileStream fileStream = compilerResults.CompiledAssembly.GetFiles()[0];
			byte[] result;
			using (MemoryStream memoryStream2 = new MemoryStream())
			{
				fileStream.CopyTo(memoryStream2);
				result = memoryStream2.ToArray();
			}
			return result;
		}

		// Token: 0x0400005A RID: 90
		private static MethodDef eraseMethod;

		// Token: 0x0400005B RID: 91
		private static MethodDef debuggerMethod;
	}
}
