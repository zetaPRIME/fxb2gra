using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Fluent.IO;
using Path = Fluent.IO.Path;
using LitJson;

namespace fxb2gra
{
	public class Program
	{
		static Path appPath;

		static JsonData pluginDb;

		static void Main(string[] args)
		{
			appPath = new Path(Assembly.GetExecutingAssembly().Location).Up();

			// load internal plugin db
			using (Stream js = Assembly.GetExecutingAssembly().GetManifestResourceStream("fxb2gra.embed_data.plugins.json"))
			{
				byte[] file = new byte[js.Length];
				js.Read(file, 0, file.Length);
				pluginDb = JsonMapper.ToObject(Encoding.UTF8.GetString(file));
			}

			// handle external db
			if (appPath.Add("plugins.json").Exists)
			{
				appPath.Combine("plugins.json").Open((FileStream fs) =>
				{
					byte[] file = new byte[fs.Length];
					fs.Read(file, 0, file.Length);
					JsonData pluginDb2 = JsonMapper.ToObject(Encoding.UTF8.GetString(file));

					foreach (KeyValuePair<string, JsonData> kvp in pluginDb2)
					{
						pluginDb[kvp.Key] = kvp.Value;
					}
				}, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

				pluginDb.Remove(".");

				// re-output
				appPath.Combine("plugins.json").Open((FileStream fs) =>
				{
					byte[] ofile = Encoding.UTF8.GetBytes(JsonMapper.ToPrettyJson(pluginDb));
					fs.Write(ofile, 0, ofile.Length);
				}, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
			}

			if (args.Length == 1)
			{
				Path inFile = new Path(args[0]);
				string ext = inFile.Extension.ToLower();
				if (ext == ".gra") Process_gra2fxb(inFile);
				else if (ext == ".fxp" || ext == ".fxb") Process_fxb2gra(inFile);
			}
			//else Process_gra2fxb(new Path("D:\\OpenMPT\\inst\\! Minihost Graphs\\test-synth1-twinklesquare2b-.gra"));
			else Process_fxb2gra(new Path("D:\\OpenMPT\\inst\\My-Synth1\\holy subbass.fxp"));
			//else Process_matchchecksum(new Path("D:\\OpenMPT\\inst\\My-Synth1\\RoughBassCmp2.gra"));

			// temp: wait for input
			Console.WriteLine("Press any key...");
			Console.ReadKey(true);
		}

		static string FindPluginFilename(string id)
		{
			if (!pluginDb.Has(id)) return "";
			if (!pluginDb[id].IsString) return "";
			return (string)pluginDb[id] + ".dll";
		}
		static string FindPluginName(string id)
		{
			if (!pluginDb.Has(id)) return "";
			if (!pluginDb[id].IsString) return "";
			return (string)pluginDb[id];
		}

		static void Process_matchchecksum(Path inFile)
		{
			UInt32 chksum = 0;
			UInt32 msum = 68221;

			byte[] mfile = null;
			inFile.Open((FileStream fs) =>
			{
				mfile = new byte[(int)fs.Length];
				fs.Read(mfile, 0, mfile.Length);
			});

			for (int pos = mfile.Length - 1; pos >= 0; pos--)
			{
				chksum += mfile[pos];
				if (chksum == msum)
				{
					Console.WriteLine("Found checksum start at " + pos);
					break;
				}
				if (chksum > msum)
				{
					Console.WriteLine("Checksum overrun at " + pos);
					break;
				}
			}
		}

		static void Process_gra2fxb(Path inFile)
		{
			byte[] fxfile = null;

			Console.WriteLine("Opening graph " + inFile.FileName);
			inFile.Open((FileStream fs) =>
			{
				BinaryReader br = new BinaryReader(fs, Encoding.ASCII);

				long fxpos = 0;

				while (true)
				{
					if (br.BaseStream.Position >= br.BaseStream.Length - 5)
					{
						Console.WriteLine("Reached EOF without finding FXP/FXB");
						return;
					}
					char c = br.ReadChar();
					if (c != 'C') continue;
					if (br.PeekChar() != 'c') continue;
					br.ReadChar();
					if (br.PeekChar() != 'n') continue;
					br.ReadChar();
					if (br.PeekChar() != 'K') continue;
					br.ReadChar();

					// found!
					fxpos = br.BaseStream.Position - 4;
					Console.WriteLine("Found FXP/FXB at offset " + fxpos);
					break;
				}

				br.BaseStream.Seek(fxpos - 8, SeekOrigin.Begin);
				long fxSize = br.ReadInt64();
				Console.WriteLine("Size: " + fxSize + " bytes");

				fxfile = br.ReadBytes((int)fxSize);
			});

			string ext = ".fxb";

			string tag = "";
			tag += Convert.ToChar(fxfile[8]);
			tag += Convert.ToChar(fxfile[9]);
			tag += Convert.ToChar(fxfile[10]);
			tag += Convert.ToChar(fxfile[11]);

			if (tag == "FPCh") ext = ".fxp";

			Path outfile = inFile.ChangeExtension(ext);
			Console.WriteLine("Writing preset file " + outfile.FileName);
			outfile.Open((FileStream fs) =>
			{
				fs.Write(fxfile, 0, fxfile.Length);
			}, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
		}

		static void Process_fxb2gra(Path inFile)
		{
			byte[] fxfile = null;

			Console.WriteLine("Opening preset file " + inFile.FileName);
			inFile.Open((FileStream fs) =>
			{
				fxfile = new byte[(int)fs.Length];
				fs.Read(fxfile, 0, fxfile.Length);
			});

			// set version to 1 so MM will load it
			fxfile[12] = 0;
			fxfile[13] = 0;
			fxfile[14] = 0;
			fxfile[15] = 1;

			string vstId = "";
			vstId += (char)fxfile[16];
			vstId += (char)fxfile[17];
			vstId += (char)fxfile[18];
			vstId += (char)fxfile[19];
			string vstName = FindPluginName(vstId);
			string vstFilename = FindPluginFilename(vstId);

			Assembly asm = Assembly.GetExecutingAssembly();

			string name = inFile.FileNameWithoutExtension;

			Path outfile = inFile.ChangeExtension(".gra");
			Console.WriteLine("Writing graph file " + outfile.FileName);
			outfile.Open((FileStream fs) =>
			{
				BinaryWriter bw = new BinaryWriter(fs);

				Stream ds;
				byte[] buffer;

				// header part 1
				ds = asm.GetManifestResourceStream("fxb2gra.embed_data.gra-header1.dat");
				buffer = new byte[(int)ds.Length];
				ds.Read(buffer, 0, buffer.Length);
				bw.Write(buffer);

				// graph name
				bw.Write((name + " (" + vstName + ")").ToCharArray());
				bw.Write((byte)0);

				// header part 2
				ds = asm.GetManifestResourceStream("fxb2gra.embed_data.gra-header2.dat");
				buffer = new byte[(int)ds.Length];
				ds.Read(buffer, 0, buffer.Length);
				bw.Write(buffer);

				// tag
				//bw.Write("\<?xml version=\"1.0\" encoding=\"UTF-8\"?>".ToCharArray());
				bw.Write(("<PLUGIN file=\"" + vstFilename + "\"/>").ToCharArray());
				bw.Write((byte)0);

				bw.Write((byte)7);
				bw.Write((byte)0);

				// length and file
				bw.Write((Int64)fxfile.Length);
				bw.Write(fxfile);

				// footer part 1
				ds = asm.GetManifestResourceStream("fxb2gra.embed_data.gra-footer1.dat");
				buffer = new byte[(int)ds.Length];
				ds.Read(buffer, 0, buffer.Length);
				bw.Write(buffer);

				// node name
				bw.Write((name + " (" + vstName + ")").ToCharArray());
				bw.Write((byte)0);

				// footer part 2
				ds = asm.GetManifestResourceStream("fxb2gra.embed_data.gra-footer2.dat");
				buffer = new byte[(int)ds.Length];
				ds.Read(buffer, 0, buffer.Length);
				bw.Write(buffer);

				// gen checksum
				bw.BaseStream.Seek(31, SeekOrigin.Begin);
				BinaryReader br = new BinaryReader(fs);
				UInt32 chksum = 0;
				while (br.BaseStream.Position < br.BaseStream.Length)
				{
					chksum += br.ReadByte();
				}
				bw.BaseStream.Seek(27, SeekOrigin.Begin);
				bw.Write(chksum);

			}, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
		}
	}
}
