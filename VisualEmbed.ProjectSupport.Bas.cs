// VisualEmbed.ProjectSupport.BaseProject
using System;
using System.IO;
using System.Xml.Serialization;

public abstract class BaseProject
{
	public static object Create(Type type, Stream theStream)
	{
		try
		{
			return new XmlSerializer(type).Deserialize(theStream);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static object Create(Type type, string theXml)
	{
		object result = null;
		try
		{
			Stream stream = new MemoryStream();
			StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.Write(theXml);
			streamWriter.Flush();
			stream.Seek(0L, SeekOrigin.Begin);
			result = Create(type, stream);
			streamWriter.Dispose();
			stream.Dispose();
			return result;
		}
		catch (Exception)
		{
			return result;
		}
	}

	public static object CreateFromFile(Type type, string theFile)
	{
		object result = null;
		try
		{
			Stream stream = new FileStream(theFile, FileMode.Open, FileAccess.Read);
			result = Create(type, stream);
			stream.Dispose();
			return result;
		}
		catch (Exception)
		{
			return result;
		}
	}

	public bool Save(Stream theStream)
	{
		try
		{
			new XmlSerializer(GetType()).Serialize(theStream, this);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool Save(out string theXml)
	{
		try
		{
			Stream stream = new MemoryStream();
			bool result = Save(stream);
			stream.Seek(0L, SeekOrigin.Begin);
			StreamReader streamReader = new StreamReader(stream);
			theXml = streamReader.ReadToEnd();
			streamReader.Dispose();
			stream.Dispose();
			return result;
		}
		catch (Exception)
		{
			theXml = string.Empty;
			return false;
		}
	}

	public bool Save(string thePrjFile)
	{
		try
		{
			Stream stream = new FileStream(thePrjFile, FileMode.Create, FileAccess.ReadWrite);
			bool result = Save(stream);
			stream.Dispose();
			return result;
		}
		catch (Exception)
		{
			return false;
		}
	}
}

// VisualEmbed.ProjectSupport.IDEPath
using System.IO;
using Microsoft.Win32;

public class IDEPath
{
	public static string MdkPath
	{
		get
		{
			string result = string.Empty;
			RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
			if (registryKey != null)
			{
				RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Keil\\Products\\MDK");
				if (registryKey2 == null)
				{
					registryKey2 = registryKey.OpenSubKey("SOFTWARE\\WOW6432Node\\Keil\\Products\\MDK");
				}
				if (registryKey2 != null)
				{
					result = (string)registryKey2.GetValue("Path", string.Empty);
					registryKey2.Close();
				}
				registryKey.Close();
			}
			return result;
		}
	}

	private static int CompareStr(string str1, string str2)
	{
		char[] array = str1.ToLower().ToCharArray();
		char[] array2 = str2.ToLower().ToCharArray();
		int num = array.Length;
		bool flag = num == array2.Length;
		if (num > array2.Length)
		{
			num = array2.Length;
		}
		int i;
		for (i = 0; i < num && array[i] == array2[i]; i++)
		{
		}
		if (i == num && flag)
		{
			i = int.MaxValue;
		}
		return i;
	}

	public static string GetValidDir(string BasePath, string SubPath)
	{
		string text = ((BasePath != null && !(BasePath == string.Empty)) ? (BasePath + "\\" + SubPath) : SubPath);
		if (!Directory.Exists(text))
		{
			string[] directories = Directory.GetDirectories(BasePath);
			int num = -1;
			string[] array = directories;
			foreach (string text2 in array)
			{
				int num2 = CompareStr(SubPath, Path.GetFileName(text2));
				if (num2 > num)
				{
					num = num2;
					text = text2;
				}
			}
		}
		return text;
	}

	public static string GetValidDir(string BasePath, params object[] Args)
	{
		string text = BasePath;
		foreach (object obj in Args)
		{
			if (obj != null)
			{
				text = GetValidDir(text, obj.ToString());
			}
		}
		return text;
	}
}

// VisualEmbed.ProjectSupport.ISupportUI
public interface ISupportUI
{
	void ShowMsg(string Msg, params object[] Args);

	void ShowError(string Msg, params object[] Args);

	void ShowWarning(string Msg, params object[] Args);

	int Select(string Msg, string[] selectList);
}

// VisualEmbed.ProjectSupport.PrjConvert
using System;
using System.Collections.Generic;
using System.IO;
using VisualEmbed.ProjectSupport;

public class PrjConvert
{
	private const int FT_CItem = 0;

	private const int FT_AsmItem = 1;

	private const int FT_ObjItem = 2;

	private const int FT_LibItem = 3;

	private const int FT_TextItem = 4;

	private const int FT_Unknown = 5;

	private const int FT_CustomItem = 6;

	private const int FT_CPPItem = 7;

	private const int FT_None = 8;

	private const int FT_IncItem = 9;

	private const int FT_END = 9;

	private static readonly Type[] FilterItemType = new Type[10]
	{
		typeof(VcxPrjFilter.VcxFltCItem),
		typeof(VcxPrjFilter.VcxFltAsmItem),
		typeof(VcxPrjFilter.VcxFltObj),
		typeof(VcxPrjFilter.VcxFltLib),
		typeof(VcxPrjFilter.VcxFltText),
		typeof(VcxPrjFilter.VcxFltNone),
		typeof(VcxPrjFilter.VcxFltCustomItem),
		typeof(VcxPrjFilter.VcxFltCItem),
		typeof(VcxPrjFilter.VcxFltNone),
		typeof(VcxPrjFilter.VcxFltInc)
	};

	private static readonly Type[] PrjFileItemType = new Type[10]
	{
		typeof(VcxProject.VcxCItem),
		typeof(VcxProject.VcxAsmItem),
		typeof(VcxProject.VcxObj),
		typeof(VcxProject.VcxLib),
		typeof(VcxProject.VcxText),
		typeof(VcxProject.VcxNone),
		typeof(VcxProject.VcxCustomItem),
		typeof(VcxProject.VcxCItem),
		typeof(VcxProject.VcxNone),
		typeof(VcxProject.VcxInc)
	};

	private static readonly string[] FileExts = new string[10]
	{
		".c;.cpp;",
		".s;.asm;",
		".o;.obj;",
		".a;.lib;",
		".txt;",
		string.Empty,
		string.Empty,
		".c;.cpp;",
		string.Empty,
		".h;.hpp;.inc;.i;"
	};

	private UvProject theUV;

	private VcxProject theVCX;

	private VcxPrjFilter theVcxFlt;

	private string ToUvCondition = string.Empty;

	private string theGlobalIncDirs = string.Empty;

	private string theGlobalMacros = string.Empty;

	private UvPack thePack;

	public ISupportUI UI { get; set; }

	public UvProject UvProject => theUV;

	public VcxProject VcxProject => theVCX;

	public VcxPrjFilter VcxFilter => theVcxFlt;

	private void ShowMsg(string Msg, params object[] Args)
	{
		if (UI != null)
		{
			UI.ShowMsg(Msg, Args);
		}
	}

	private void ShowError(string Msg, params object[] Args)
	{
		if (UI != null)
		{
			UI.ShowError(Msg, Args);
		}
	}

	private void ShowWarning(string Msg, params object[] Args)
	{
		if (UI != null)
		{
			UI.ShowWarning(Msg, Args);
		}
	}

	private int Select(string Msg, string[] selectList)
	{
		if (UI == null)
		{
			return 0;
		}
		return UI.Select(Msg, selectList);
	}

	private static int UvFileTypeToVcxIdx(int theUvFileType, string FileName)
	{
		switch (theUvFileType)
		{
		default:
			return 8;
		case 4:
		{
			string text = Path.GetExtension(FileName).ToLower();
			if (text == ".h" || text == ".hpp")
			{
				return 9;
			}
			break;
		}
		case 1:
		case 2:
		case 3:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
			break;
		}
		return theUvFileType - 1;
	}

	private static int FileTypeToVcxIdx(string FileName)
	{
		string value = Path.GetExtension(FileName).ToLower() + ";";
		for (int i = 0; i < FileExts.Length; i++)
		{
			if (FileExts[i].IndexOf(value) >= 0)
			{
				return i;
			}
		}
		return 8;
	}

	private static int FileTypeToUvType(Type type, bool IsFilter)
	{
		int num = 1;
		bool flag = false;
		Type[] array = (IsFilter ? FilterItemType : PrjFileItemType);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == type)
			{
				flag = true;
				break;
			}
			num++;
		}
		if (!flag)
		{
			num = 4;
		}
		else if (num == 9)
		{
			num = 4;
		}
		return num;
	}

	private static string AdjustMacrosForUV(string theMacros)
	{
		if (theMacros == null)
		{
			return string.Empty;
		}
		char[] array = theMacros.ToCharArray();
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < array.Length; i++)
		{
			switch (array[i])
			{
			case '"':
				if (!flag)
				{
					flag2 = !flag2;
				}
				else
				{
					flag = false;
				}
				break;
			case '\\':
				if (flag2)
				{
					flag = !flag;
				}
				break;
			case ';':
				if (!flag && !flag2)
				{
					array[i] = ' ';
				}
				else if (flag)
				{
					flag = false;
				}
				break;
			case ' ':
			case ',':
				if ((flag || flag2) && flag)
				{
					flag = false;
				}
				break;
			default:
				if (flag)
				{
					flag = false;
				}
				break;
			}
		}
		return new string(array);
	}

	private static string AdjustPathForUV(string thePath)
	{
		return thePath.Replace("$(veDefMDKPath)", IDEPath.MdkPath).Replace("$(vePrjPath)", ".\\");
	}

	private static bool VcxToUvLocateProp(VcxProject.VcxProps Props, string Type, string theCond)
	{
		bool isContinue = false;
		while (isContinue = Props.Walkthrough(isContinue))
		{
			if (Props.Type == Type)
			{
				if (theCond == null)
				{
					return true;
				}
				if (Props.GetAttribute(VcxProject.AttrCond).Replace(" ", string.Empty) == theCond.Replace(" ", string.Empty))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static int VcxBoolPropsToUv(VcxProject.VcxProps Props, string Type, string theCond, int Default)
	{
		if (VcxToUvLocateProp(Props, Type, theCond))
		{
			if (!(Props.Value.ToLower().Trim() == "true"))
			{
				return 0;
			}
			return 1;
		}
		return Default;
	}

	private static int VcxBoolPropsToUv(VcxProject.VcxProps Props, string Type, string theCond, int TrueVal, int FalseVal, int Default)
	{
		if (VcxToUvLocateProp(Props, Type, theCond))
		{
			if (!(Props.Value.ToLower().Trim() == "true"))
			{
				return FalseVal;
			}
			return TrueVal;
		}
		return Default;
	}

	private static int VcxIntPropsToUv(VcxProject.VcxProps Props, string Type, string theCond, int Default)
	{
		if (VcxToUvLocateProp(Props, Type, theCond))
		{
			return int.Parse(Props.Value);
		}
		return Default;
	}

	private static string VcxStrPropsToUv(VcxProject.VcxProps Props, string Type, string theCond, string Default)
	{
		if (VcxToUvLocateProp(Props, Type, theCond))
		{
			return Props.Value;
		}
		return Default;
	}

	private void VcxToUvCAttr(VcxProject.VcxProps Props, UvProject.UvArmAds ArmAds, string theCond)
	{
		ArmAds.Cads.VariousControls.Define = AdjustMacrosForUV(theGlobalMacros + VcxStrPropsToUv(Props, "veCCMacros", theCond, string.Empty));
		ArmAds.Cads.VariousControls.IncludePath = theGlobalIncDirs + VcxStrPropsToUv(Props, "veCCIncDir", theCond, string.Empty);
		ArmAds.Cads.VariousControls.IncludePath = AdjustPathForUV(ArmAds.Cads.VariousControls.IncludePath);
		ArmAds.Cads.interw = VcxBoolPropsToUv(Props, "veCCInterwork", theCond, 1);
		ArmAds.Cads.Optim = VcxIntPropsToUv(Props, "veCCOptimize", theCond, -1) + 1;
		ArmAds.Cads.oTime = VcxIntPropsToUv(Props, "veCCOptType", theCond, 0);
		ArmAds.Cads.useXO = VcxBoolPropsToUv(Props, "veCCExecOnly", theCond, 0);
		ArmAds.Cads.SplitLS = VcxBoolPropsToUv(Props, "veCCSplitLdm", theCond, 0);
		ArmAds.Cads.OneElfS = VcxBoolPropsToUv(Props, "veCCSplitSec", theCond, 1);
		ArmAds.Cads.Strict = VcxBoolPropsToUv(Props, "veCCStrict", theCond, 1);
		ArmAds.Cads.uC99 = VcxBoolPropsToUv(Props, "veCC99", theCond, 0);
		ArmAds.Cads.EnumInt = VcxBoolPropsToUv(Props, "veCCEnumAsInt", theCond, 0);
		ArmAds.Cads.PlainCh = VcxBoolPropsToUv(Props, "veCCSignedChar", theCond, 0);
		ArmAds.Cads.Ropi = VcxBoolPropsToUv(Props, "veCCRopi", theCond, 0);
		ArmAds.Cads.Rwpi = VcxBoolPropsToUv(Props, "veCCRwpi", theCond, 0);
		ArmAds.Cads.wLevel = ((VcxIntPropsToUv(Props, "veCCWarn", theCond, 3) != 3) ? 1 : 0);
		ArmAds.Cads.uThumb = VcxBoolPropsToUv(Props, "veCCThumb", theCond, 0);
		ArmAds.Cads.VariousControls.MiscControls = VcxStrPropsToUv(Props, "veCCMisc", theCond, string.Empty);
		if (ArmAds.ArmAdsMisc != null)
		{
			ArmAds.ArmAdsMisc.RvctClst = VcxBoolPropsToUv(Props, "veCCList", theCond, 0);
		}
	}

	private void VcxToUvAsmAttr(VcxProject.VcxProps Props, UvProject.UvArmAds ArmAds, string theCond)
	{
		ArmAds.Aads.VariousControls.Define = AdjustMacrosForUV(theGlobalMacros + VcxStrPropsToUv(Props, "veASMacros", theCond, string.Empty));
		ArmAds.Aads.VariousControls.IncludePath = theGlobalIncDirs + VcxStrPropsToUv(Props, "veASIncDir", theCond, string.Empty);
		ArmAds.Aads.VariousControls.IncludePath = AdjustPathForUV(ArmAds.Aads.VariousControls.IncludePath);
		ArmAds.Aads.NoWarn = ((VcxIntPropsToUv(Props, "veASWarn", theCond, 3) != 3) ? 1 : 0);
		ArmAds.Aads.interw = VcxBoolPropsToUv(Props, "veASInterwork", theCond, 1);
		ArmAds.Aads.thumb = VcxBoolPropsToUv(Props, "veASThumb", theCond, 0);
		ArmAds.Aads.useXO = VcxBoolPropsToUv(Props, "veASExecOnly", theCond, 0);
		ArmAds.Aads.SplitLS = VcxBoolPropsToUv(Props, "veASSplitLdm", theCond, 0);
		ArmAds.Aads.Ropi = VcxBoolPropsToUv(Props, "veASRopi", theCond, 0);
		ArmAds.Aads.Rwpi = VcxBoolPropsToUv(Props, "veASRwpi", theCond, 0);
		ArmAds.Aads.VariousControls.MiscControls = VcxStrPropsToUv(Props, "veASMisc", theCond, string.Empty);
		if (ArmAds.ArmAdsMisc != null)
		{
			ArmAds.ArmAdsMisc.AdsALst = VcxBoolPropsToUv(Props, "veASList", theCond, 0);
			ArmAds.ArmAdsMisc.AdsACrf = VcxBoolPropsToUv(Props, "veASXref", theCond, 0);
		}
	}

	private void VcxToUvCustomAttr(VcxProject.VcxProps Props, UvProject.UvCommonProperty Property, string theCond)
	{
		Property.CustomArgument = VcxStrPropsToUv(Props, "BuildCmd", theCond, string.Empty);
	}

	private void VcxToUvLinkAttr(VcxProject.VcxProps Props, UvProject.UvArmAds ArmAds, string theCond)
	{
		ArmAds.LDads.ScatterFile = VcxStrPropsToUv(Props, "veLnScatter", theCond, string.Empty);
		ArmAds.LDads.noStLib = VcxBoolPropsToUv(Props, "veLnNoStdLib", theCond, 0);
		ArmAds.LDads.RepFail = VcxBoolPropsToUv(Props, "veLnStrict", theCond, 1);
		ArmAds.ArmAdsMisc.AdsLmap = VcxBoolPropsToUv(Props, "veLnMap", theCond, 1);
		ArmAds.ArmAdsMisc.AdsLsxf = VcxBoolPropsToUv(Props, "veLnXref", theCond, 1);
		ArmAds.ArmAdsMisc.AdsLcgr = VcxBoolPropsToUv(Props, "veLnCallgraph", theCond, 1);
		ArmAds.ArmAdsMisc.AdsLsym = VcxBoolPropsToUv(Props, "veLnSymbols", theCond, 1);
		ArmAds.ArmAdsMisc.AdsLszi = VcxBoolPropsToUv(Props, "veLnInfoSizes", theCond, 1);
		ArmAds.ArmAdsMisc.AdsLtoi = VcxBoolPropsToUv(Props, "veLnInfoTotals", theCond, 1);
		ArmAds.ArmAdsMisc.AdsLsun = VcxBoolPropsToUv(Props, "veLnInfoUnused", theCond, 1);
		ArmAds.ArmAdsMisc.AdsLven = VcxBoolPropsToUv(Props, "veLnInfoVeneers", theCond, 1);
		ArmAds.LDads.DisabledWarnings = VcxStrPropsToUv(Props, "veLnSuppress", theCond, string.Empty);
		ArmAds.LDads.Misc = VcxStrPropsToUv(Props, "veLnMisc", theCond, string.Empty);
	}

	private void VcxToUvFileCommonAttr(VcxProject.VcxProps Props, UvProject.UvCommonProperty Property, string theCond)
	{
		Property.IncludeInBuild = VcxBoolPropsToUv(Props, "ExcludedFromBuild", theCond, 0, 1, 1);
	}

	private void VcxToUvPrjGeneralAttr(VcxProject.VcxProps Props, UvProject.UvTargetOption Property, string theCond)
	{
		Property.TargetCommonOption.OutputDirectory = VcxStrPropsToUv(Props, "veObjDir", theCond, string.Empty) + "\\";
		Property.TargetCommonOption.ListingPath = VcxStrPropsToUv(Props, "veListDir", theCond, string.Empty) + "\\";
		Property.TargetArmAds.ArmAdsMisc.AdsCpuType = VcxStrPropsToUv(Props, "veCPUType", theCond, string.Empty);
		string text = VcxStrPropsToUv(Props, "veTargetType", theCond, "app").ToLower().Trim();
		Property.TargetCommonOption.CreateExecutable = ((text == "app") ? 1 : 0);
		Property.TargetCommonOption.CreateLib = ((text == "lib") ? 1 : 0);
		text = VcxStrPropsToUv(Props, "veTargetName", theCond, null);
		if (text != null)
		{
			Property.TargetCommonOption.OutputName = Props.Value;
		}
		text = VcxStrPropsToUv(Props, "veTargetExt", theCond, null);
		if (text != null)
		{
			if (!text.StartsWith("."))
			{
				Property.TargetCommonOption.OutputName += ".";
			}
			Property.TargetCommonOption.OutputName += text;
		}
		Property.TargetCommonOption.DebugInformation = 1;
		theGlobalIncDirs = VcxStrPropsToUv(Props, "veStdIncDir", theCond, string.Empty);
		if (VcxStrPropsToUv(Props, "veCommonIncDir", theCond, string.Empty) != string.Empty)
		{
			if (theGlobalIncDirs != string.Empty || !theGlobalIncDirs.EndsWith(";"))
			{
				theGlobalIncDirs += " ";
			}
			theGlobalIncDirs += VcxStrPropsToUv(Props, "veCommonIncDir", theCond, string.Empty);
		}
		if (theGlobalIncDirs != string.Empty || !theGlobalIncDirs.EndsWith(";"))
		{
			theGlobalIncDirs += " ";
		}
		theGlobalMacros = VcxStrPropsToUv(Props, "veCommonMacros", theCond, string.Empty);
		if (theGlobalMacros != string.Empty || !theGlobalMacros.EndsWith(";"))
		{
			theGlobalMacros += " ";
		}
	}

	private void VcxToUvPrjToolsetAttr(VcxProject.VcxProps Props, UvProject.UvTarget target, string theCond)
	{
		target.ToolsetName = "ARM-ADS";
		target.ToolsetNumber = "0x4";
	}

	private void VcxToUvUVisionAttr(VcxProject.VcxProps Props, UvProject.UvTarget Target, string theCond)
	{
		Target.TargetOption.TargetCommonOption.Device = VcxStrPropsToUv(Props, "uv_Device", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.Vendor = VcxStrPropsToUv(Props, "uv_Vendor", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.PackID = VcxStrPropsToUv(Props, "uv_PackID", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.PackURL = VcxStrPropsToUv(Props, "uv_PackURL", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.Cpu = VcxStrPropsToUv(Props, "uv_Cpu", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.FlashUtilSpec = VcxStrPropsToUv(Props, "uv_FlashUtilSpec", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.StartupFile = VcxStrPropsToUv(Props, "uv_StartupFile", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.FlashDriverDll = VcxStrPropsToUv(Props, "uv_FlashDriverDll", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.DeviceId = VcxStrPropsToUv(Props, "uv_DeviceId", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.RegisterFile = VcxStrPropsToUv(Props, "uv_RegisterFile", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.MemoryEnv = VcxStrPropsToUv(Props, "uv_MemoryEnv", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.Cmp = VcxStrPropsToUv(Props, "uv_Cmp", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.Asm = VcxStrPropsToUv(Props, "uv_Asm", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.Linker = VcxStrPropsToUv(Props, "uv_Linker", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.OHString = VcxStrPropsToUv(Props, "uv_OHString", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.InfinionOptionDll = VcxStrPropsToUv(Props, "uv_InfinionOptionDll", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.SLE66CMisc = VcxStrPropsToUv(Props, "uv_SLE66CMisc", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.SLE66AMisc = VcxStrPropsToUv(Props, "uv_SLE66AMisc", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.SLE66LinkerMisc = VcxStrPropsToUv(Props, "uv_SLE66LinkerMisc", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.SFDFile = VcxStrPropsToUv(Props, "uv_SFDFile", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.bCustSvd = VcxIntPropsToUv(Props, "uv_bCustSvd", theCond, 0);
		Target.TargetOption.TargetCommonOption.UseEnv = VcxIntPropsToUv(Props, "uv_UseEnv", theCond, 0);
		Target.TargetOption.TargetCommonOption.BinPath = VcxStrPropsToUv(Props, "uv_BinPath", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.IncludePath = VcxStrPropsToUv(Props, "uv_IncludePath", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.LibPath = VcxStrPropsToUv(Props, "uv_LibPath", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.RegisterFilePath = VcxStrPropsToUv(Props, "uv_RegisterFilePath", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.DBRegisterFilePath = VcxStrPropsToUv(Props, "uv_DBRegisterFilePath", theCond, string.Empty);
		Target.TargetOption.TargetCommonOption.TargetStatus = new UvProject.UvTargetStatus();
		Target.TargetOption.TargetCommonOption.TargetStatus.Error = 0;
		Target.TargetOption.TargetCommonOption.TargetStatus.ExitCodeStop = 0;
		Target.TargetOption.TargetCommonOption.TargetStatus.InvalidFlash = 1;
		Target.TargetOption.TargetCommonOption.TargetStatus.NotGenerated = 0;
		Target.TargetOption.TargetCommonOption.TargetStatus.ButtonStop = 0;
	}

	private void VcxUesrCmdToUv(VcxProject.VcxProps Props, string Type, string theCond, out string Cmd1, out int IsRun1, out string Cmd2, out int IsRun2)
	{
		Cmd1 = string.Empty;
		Cmd2 = string.Empty;
		IsRun1 = 0;
		IsRun2 = 0;
		string[] array = VcxStrPropsToUv(Props, Type, theCond, string.Empty).Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 0)
		{
			if (!array[0].StartsWith("*"))
			{
				IsRun1 = 1;
				Cmd1 = array[0];
			}
			else
			{
				Cmd1 = array[0].Substring(1);
			}
		}
		if (array.Length > 1)
		{
			if (!array[1].StartsWith("*"))
			{
				IsRun2 = 1;
				Cmd2 = array[1];
			}
			else
			{
				Cmd2 = array[1].Substring(1);
			}
		}
	}

	private void VcxToUvUserCmdAttr(VcxProject.VcxProps Props, UvProject.UvTargetOption Property, string theCond)
	{
		VcxUesrCmdToUv(Props, "veUsrBeforeFileProc", theCond, out Property.TargetCommonOption.BeforeCompile.UserProg1Name, out Property.TargetCommonOption.BeforeCompile.RunUserProg1, out Property.TargetCommonOption.BeforeCompile.UserProg2Name, out Property.TargetCommonOption.BeforeCompile.RunUserProg2);
		VcxUesrCmdToUv(Props, "veUsrBeforeBuildProc", theCond, out Property.TargetCommonOption.BeforeMake.UserProg1Name, out Property.TargetCommonOption.BeforeMake.RunUserProg1, out Property.TargetCommonOption.BeforeMake.UserProg2Name, out Property.TargetCommonOption.BeforeMake.RunUserProg2);
		VcxUesrCmdToUv(Props, "veUsrAfterBuildProc", theCond, out Property.TargetCommonOption.AfterMake.UserProg1Name, out Property.TargetCommonOption.AfterMake.RunUserProg1, out Property.TargetCommonOption.AfterMake.UserProg2Name, out Property.TargetCommonOption.AfterMake.RunUserProg2);
	}

	private VcxProject.VcxProps VcxToUvPickProps(string PropsLabel, string theCond)
	{
		VcxProject.VcxProps result = null;
		try
		{
			foreach (VcxProject.VcxPropertyGroup propertyGroup in theVCX.PropertyGroups)
			{
				if (propertyGroup.Label == PropsLabel && (((propertyGroup.Condition == null || propertyGroup.Condition == string.Empty) && (theCond == null || theCond == string.Empty)) || propertyGroup.Condition == theCond))
				{
					result = propertyGroup.Props;
					return result;
				}
			}
			return result;
		}
		catch (Exception)
		{
			return result;
		}
	}

	private bool VcxToUvTargetAttr(UvProject.UvTarget target)
	{
		try
		{
			VcxProject.VcxProps vcxProps = VcxToUvPickProps("arm.ve.props.general", null);
			if (vcxProps != null)
			{
				VcxToUvPrjToolsetAttr(vcxProps, target, null);
				VcxToUvPrjGeneralAttr(vcxProps, target.TargetOption, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.general", ToUvCondition);
			if (vcxProps != null)
			{
				VcxToUvPrjToolsetAttr(vcxProps, target, null);
				VcxToUvPrjGeneralAttr(vcxProps, target.TargetOption, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.uvision", null);
			if (vcxProps != null)
			{
				VcxToUvUVisionAttr(vcxProps, target, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.uvision", ToUvCondition);
			if (vcxProps != null)
			{
				VcxToUvUVisionAttr(vcxProps, target, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.cc", null);
			if (vcxProps != null)
			{
				VcxToUvCAttr(vcxProps, target.TargetOption.TargetArmAds, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.cc", ToUvCondition);
			if (vcxProps != null)
			{
				VcxToUvCAttr(vcxProps, target.TargetOption.TargetArmAds, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.asm", null);
			if (vcxProps != null)
			{
				VcxToUvAsmAttr(vcxProps, target.TargetOption.TargetArmAds, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.asm", ToUvCondition);
			if (vcxProps != null)
			{
				VcxToUvAsmAttr(vcxProps, target.TargetOption.TargetArmAds, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.link", null);
			if (vcxProps != null)
			{
				VcxToUvLinkAttr(vcxProps, target.TargetOption.TargetArmAds, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.link", ToUvCondition);
			if (vcxProps != null)
			{
				VcxToUvLinkAttr(vcxProps, target.TargetOption.TargetArmAds, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.customcmd", null);
			if (vcxProps != null)
			{
				VcxToUvUserCmdAttr(vcxProps, target.TargetOption, null);
			}
			vcxProps = VcxToUvPickProps("arm.ve.props.customcmd", ToUvCondition);
			if (vcxProps != null)
			{
				VcxToUvUserCmdAttr(vcxProps, target.TargetOption, null);
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private void VcxToUvFileAttr(UvProject.UvFile file, string theCond)
	{
		object obj = null;
		foreach (object item in theVCX.ItemGroup)
		{
			if (!(item is VcxProject.VcxProjectConfiguration) && file.FilePath.ToLower() == (item as VcxProject.VcxFileItem).Include.ToLower())
			{
				obj = item;
				break;
			}
		}
		if (obj == null)
		{
			return;
		}
		VcxProject.VcxFileItem vcxFileItem = obj as VcxProject.VcxFileItem;
		bool isContinue = false;
		bool flag = false;
		while (isContinue = vcxFileItem.Metadatas.Walkthrough(isContinue))
		{
			if (vcxFileItem.Metadatas.Type == "CustomCfg" || vcxFileItem.Metadatas.Type == "ExcludedFromBuild")
			{
				flag = true;
				if (vcxFileItem.Metadatas.GetAttribute(VcxProject.AttrCond).Replace(" ", string.Empty) == theCond.Replace(" ", string.Empty))
				{
					break;
				}
			}
		}
		if (flag)
		{
			file.FileOption = new UvProject.UvFileOption();
			file.FileOption.CommonProperty = new UvProject.UvCommonProperty();
			VcxToUvFileCommonAttr(vcxFileItem.Metadatas, file.FileOption.CommonProperty, null);
			VcxToUvFileCommonAttr(vcxFileItem.Metadatas, file.FileOption.CommonProperty, theCond);
			if (obj is VcxProject.VcxCItem)
			{
				file.FileOption.FileArmAds = new UvProject.UvArmAds
				{
					ArmAdsMisc = null,
					Cads = new UvProject.UvCads(),
					Aads = null,
					LDads = null
				};
				VcxToUvCAttr(vcxFileItem.Metadatas, file.FileOption.FileArmAds, null);
				VcxToUvCAttr(vcxFileItem.Metadatas, file.FileOption.FileArmAds, theCond);
			}
			else if (obj is VcxProject.VcxAsmItem)
			{
				file.FileOption.FileArmAds = new UvProject.UvArmAds
				{
					ArmAdsMisc = null,
					Cads = null,
					Aads = new UvProject.UvAads(),
					LDads = null
				};
				VcxToUvAsmAttr(vcxFileItem.Metadatas, file.FileOption.FileArmAds, null);
				VcxToUvAsmAttr(vcxFileItem.Metadatas, file.FileOption.FileArmAds, theCond);
			}
			else if (obj is VcxProject.VcxCustomItem)
			{
				VcxToUvCustomAttr(vcxFileItem.Metadatas, file.FileOption.CommonProperty, null);
				VcxToUvCustomAttr(vcxFileItem.Metadatas, file.FileOption.CommonProperty, theCond);
			}
		}
	}

	private bool VcxToUvProcFile(UvProject.UvGroup group)
	{
		try
		{
			bool flag = true;
			foreach (object item in theVcxFlt.ItemGroup)
			{
				if (item is VcxPrjFilter.VcxFilter)
				{
					continue;
				}
				VcxPrjFilter.VcxFltFileItem vcxFltFileItem = item as VcxPrjFilter.VcxFltFileItem;
				if (vcxFltFileItem.Filter == group.GroupName)
				{
					flag = theUV.AddFile(group, vcxFltFileItem.Include, FileTypeToUvType(item.GetType(), IsFilter: true), out var theFile);
					if (!flag)
					{
						return flag;
					}
					VcxToUvFileAttr(theFile, ToUvCondition);
				}
			}
			return flag;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private bool VcxToUvProcGroup(UvProject.UvTarget target)
	{
		try
		{
			bool flag = true;
			foreach (object item in theVcxFlt.ItemGroup)
			{
				if (item is VcxPrjFilter.VcxFilter)
				{
					flag = theUV.AddGroup(target, (item as VcxPrjFilter.VcxFilter).Include, out var theGroup);
					if (!flag)
					{
						return flag;
					}
					VcxToUvProcFile(theGroup);
				}
			}
			return flag;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private void VcxToUvCleanup()
	{
		List<int> list = new List<int>();
		foreach (UvProject.UvTarget target in theUV.Targets)
		{
			int num = 0;
			foreach (UvProject.UvGroup group in target.Groups)
			{
				if (group.Files.Count <= 0 && !group.GroupName.StartsWith("::"))
				{
					list.Add(num);
				}
				else if (group.GroupName.StartsWith("::"))
				{
					group.GroupName = group.GroupName.Replace("::", "+");
				}
				num++;
			}
			list.Reverse();
			foreach (int item in list)
			{
				target.Groups.RemoveAt(item);
			}
			list.Clear();
		}
	}

	public bool VcxToUvPrj(string VcxPrjFileName)
	{
		try
		{
			bool flag = true;
			theVCX = VcxProject.CreateFromFile(VcxPrjFileName);
			theVcxFlt = VcxPrjFilter.CreateFromFile(VcxPrjFileName.Trim() + ".filters");
			theUV = new UvProject();
			theUV.SchemaVersion = "2.1";
			theUV.Header = "### uVision Project, (C) Keil Software";
			foreach (object item in theVCX.ItemGroup)
			{
				if (item is VcxProject.VcxProjectConfiguration)
				{
					VcxProject.VcxProjectConfiguration vcxProjectConfiguration = item as VcxProject.VcxProjectConfiguration;
					ToUvCondition = $"'$(Configuration)|$(Platform)'=='{vcxProjectConfiguration.Configuration}|{vcxProjectConfiguration.Platform}'";
					flag = theUV.AddTarget($"{vcxProjectConfiguration.Configuration}.{vcxProjectConfiguration.Platform}", out var theTarget);
					if (!flag)
					{
						break;
					}
					flag = VcxToUvProcGroup(theTarget);
					if (!flag)
					{
						break;
					}
					theTarget.TargetOption.TargetCommonOption.OutputName = theTarget.TargetName;
					theTarget.TargetOption.TargetArmAds = new UvProject.UvArmAds
					{
						ArmAdsMisc = new UvProject.UvArmAdsMisc(),
						Cads = new UvProject.UvCads(),
						Aads = new UvProject.UvAads(),
						LDads = new UvProject.UvLDads()
					};
					VcxToUvTargetAttr(theTarget);
				}
			}
			VcxToUvCleanup();
			return flag;
		}
		catch (Exception ex)
		{
			ShowError("转换时出现异常：{0}", ex.Message);
			return false;
		}
	}

	private static string GetSimilarMdkPath(string BasePath, params object[] Args)
	{
		string mdkPath = IDEPath.MdkPath;
		string basePath = mdkPath + "\\" + BasePath;
		basePath = IDEPath.GetValidDir(basePath, Args);
		int num = mdkPath.Length;
		if (mdkPath.EndsWith("\\") || mdkPath.EndsWith("/"))
		{
			num--;
		}
		return "$(veDefMDKPath)" + basePath.Substring(num);
	}

	private static string AdjustMacrosForVcx(string theMacros)
	{
		if (theMacros == null)
		{
			return string.Empty;
		}
		char[] array = theMacros.ToCharArray();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = true;
		for (int i = 0; i < array.Length; i++)
		{
			switch (array[i])
			{
			case '"':
				if (!flag)
				{
					flag2 = !flag2;
				}
				else
				{
					flag = false;
				}
				flag3 = false;
				break;
			case '\\':
				if (flag2)
				{
					flag = !flag;
				}
				flag3 = false;
				break;
			case ' ':
			case ',':
				if (!flag && !flag2)
				{
					if (!flag3)
					{
						array[i] = ';';
					}
					flag3 = true;
				}
				else
				{
					if (flag)
					{
						flag = false;
					}
					flag3 = false;
				}
				break;
			case ';':
				if (!flag && !flag2)
				{
					flag3 = true;
					break;
				}
				if (flag)
				{
					flag = false;
				}
				flag3 = false;
				break;
			default:
				if (flag)
				{
					flag = false;
				}
				flag3 = false;
				break;
			}
		}
		return new string(array);
	}

	private string UvToVcxPickStdIncDir(UvProject.UvTarget Target, UvPack thePack)
	{
		string text = string.Empty;
		if (thePack != null)
		{
			UvPack.DeviceType deviceType = thePack.FindDevice(Target.TargetOption.TargetCommonOption.Device);
			if (deviceType != null)
			{
				foreach (object prop in deviceType.props)
				{
					if (!(prop is UvPack.CompileType))
					{
						continue;
					}
					string header = (prop as UvPack.CompileType).header;
					if (header != null && header != string.Empty)
					{
						header = $"$(veDefMDKPath)\\{thePack.PackRelativeRoot}\\{Path.GetDirectoryName(header)};";
						if (text.IndexOf(header) < 0)
						{
							text += header;
						}
					}
				}
			}
		}
		foreach (UvProject.UvRTEComponent component in theUV.RTE.components)
		{
			foreach (UvProject.UvRTETargetInfo targetInfo in component.targetInfos)
			{
				if (targetInfo.name.Trim() == Target.TargetName.Trim())
				{
					string header = GetSimilarMdkPath("Pack", component.package.vendor, component.package.name, component.package.version, component.Cclass, "Include") + ";";
					if (text.IndexOf(header) < 0)
					{
						text += header;
					}
					break;
				}
			}
		}
		if (!(text == string.Empty))
		{
			return text;
		}
		return null;
	}

	private string UvToVcxPickCommonIncDir(UvProject.UvTarget Target, UvPack thePack)
	{
		string text = string.Empty;
		if (theUV.RTE.components.Count > 0)
		{
			string text2 = "$(vePrjPath)RTE;";
			if (text.IndexOf(text2) < 0)
			{
				text += text2;
			}
		}
		foreach (UvProject.UvRTEFile file in theUV.RTE.files)
		{
			if (!(file.category.ToLower().Trim() == "header"))
			{
				continue;
			}
			using List<UvProject.UvRTETargetInfo>.Enumerator enumerator2 = file.targetInfos.GetEnumerator();
			if (enumerator2.MoveNext() && enumerator2.Current.name.Trim() == Target.TargetName.Trim())
			{
				string text2 = Path.GetDirectoryName(file.instance) + ";";
				if (!Path.IsPathRooted(text2) && !text2.StartsWith("."))
				{
					text2 = "$(vePrjPath)" + text2;
				}
				if (text.IndexOf(text2) < 0)
				{
					text += text2;
				}
			}
		}
		if (!(text == string.Empty))
		{
			return text;
		}
		return null;
	}

	private string UvToVcxPickCommonMacros(UvProject.UvTarget Target, UvPack thePack)
	{
		string text = string.Empty;
		if (thePack != null)
		{
			UvPack.DeviceType deviceType = thePack.FindDevice(Target.TargetOption.TargetCommonOption.Device);
			if (deviceType != null)
			{
				foreach (object prop in deviceType.props)
				{
					if (prop is UvPack.CompileType)
					{
						string text2 = AdjustMacrosForVcx((prop as UvPack.CompileType).define);
						if (text2 != null && text2 != string.Empty)
						{
							text = text + text2 + ";";
						}
					}
				}
			}
		}
		if (theUV.RTE.components.Count > 0)
		{
			text += "_RTE_;";
		}
		if (!(text == string.Empty))
		{
			return text;
		}
		return null;
	}

	private bool UvToVcxUVisionProps(UvProject.UvTarget Target, string theCond)
	{
		try
		{
			VcxProject.VcxPropertyGroup vcxPropertyGroup = new VcxProject.VcxPropertyGroup();
			vcxPropertyGroup.Condition = theCond;
			vcxPropertyGroup.Label = "arm.ve.props.uvision";
			vcxPropertyGroup.Props.AddItem("uv_Device", Target.TargetOption.TargetCommonOption.Device);
			vcxPropertyGroup.Props.AddItem("uv_Vendor", Target.TargetOption.TargetCommonOption.Vendor);
			vcxPropertyGroup.Props.AddItem("uv_PackID", Target.TargetOption.TargetCommonOption.PackID);
			vcxPropertyGroup.Props.AddItem("uv_PackURL", Target.TargetOption.TargetCommonOption.PackURL);
			vcxPropertyGroup.Props.AddItem("uv_Cpu", Target.TargetOption.TargetCommonOption.Cpu);
			vcxPropertyGroup.Props.AddItem("uv_FlashUtilSpec", Target.TargetOption.TargetCommonOption.FlashUtilSpec);
			vcxPropertyGroup.Props.AddItem("uv_StartupFile", Target.TargetOption.TargetCommonOption.StartupFile);
			vcxPropertyGroup.Props.AddItem("uv_FlashDriverDll", Target.TargetOption.TargetCommonOption.FlashDriverDll);
			vcxPropertyGroup.Props.AddItem("uv_DeviceId", Target.TargetOption.TargetCommonOption.DeviceId);
			vcxPropertyGroup.Props.AddItem("uv_RegisterFile", Target.TargetOption.TargetCommonOption.RegisterFile);
			vcxPropertyGroup.Props.AddItem("uv_MemoryEnv", Target.TargetOption.TargetCommonOption.MemoryEnv);
			vcxPropertyGroup.Props.AddItem("uv_Cmp", Target.TargetOption.TargetCommonOption.Cmp);
			vcxPropertyGroup.Props.AddItem("uv_Asm", Target.TargetOption.TargetCommonOption.Asm);
			vcxPropertyGroup.Props.AddItem("uv_Linker", Target.TargetOption.TargetCommonOption.Linker);
			vcxPropertyGroup.Props.AddItem("uv_OHString", Target.TargetOption.TargetCommonOption.OHString);
			vcxPropertyGroup.Props.AddItem("uv_InfinionOptionDll", Target.TargetOption.TargetCommonOption.InfinionOptionDll);
			vcxPropertyGroup.Props.AddItem("uv_SLE66CMisc", Target.TargetOption.TargetCommonOption.SLE66CMisc);
			vcxPropertyGroup.Props.AddItem("uv_SLE66AMisc", Target.TargetOption.TargetCommonOption.SLE66AMisc);
			vcxPropertyGroup.Props.AddItem("uv_SLE66LinkerMisc", Target.TargetOption.TargetCommonOption.SLE66LinkerMisc);
			vcxPropertyGroup.Props.AddItem("uv_SFDFile", Target.TargetOption.TargetCommonOption.SFDFile);
			vcxPropertyGroup.Props.AddItem("uv_bCustSvd", Target.TargetOption.TargetCommonOption.bCustSvd);
			vcxPropertyGroup.Props.AddItem("uv_UseEnv", Target.TargetOption.TargetCommonOption.UseEnv);
			vcxPropertyGroup.Props.AddItem("uv_BinPath", Target.TargetOption.TargetCommonOption.BinPath);
			vcxPropertyGroup.Props.AddItem("uv_IncludePath", Target.TargetOption.TargetCommonOption.IncludePath);
			vcxPropertyGroup.Props.AddItem("uv_LibPath", Target.TargetOption.TargetCommonOption.LibPath);
			vcxPropertyGroup.Props.AddItem("uv_RegisterFilePath", Target.TargetOption.TargetCommonOption.RegisterFilePath);
			vcxPropertyGroup.Props.AddItem("uv_DBRegisterFilePath", Target.TargetOption.TargetCommonOption.DBRegisterFilePath);
			theVCX.PropertyGroups.Add(vcxPropertyGroup);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private bool UvToVcxGeneralProps(UvProject.UvTarget Target, string theCond)
	{
		try
		{
			if (Target.TargetOption.TargetCommonOption.PackID != null)
			{
				thePack = UvPack.CreateFromID(Target.TargetOption.TargetCommonOption.PackID);
			}
			VcxProject.VcxPropertyGroup vcxPropertyGroup = new VcxProject.VcxPropertyGroup();
			vcxPropertyGroup.Condition = theCond;
			vcxPropertyGroup.Label = "arm.ve.props.general";
			vcxPropertyGroup.Props.AddItem("veToolChain", (Target.ToolsetName.ToUpper().Trim() == "ARM-ADS") ? "RVCT" : "GCC");
			vcxPropertyGroup.Props.AddItem("veTargetName", Path.GetFileNameWithoutExtension(Target.TargetOption.TargetCommonOption.OutputName));
			string text = Path.GetExtension(Target.TargetOption.TargetCommonOption.OutputName);
			if (text == string.Empty)
			{
				text = ((Target.TargetOption.TargetCommonOption.CreateLib == 0) ? "axf" : "a");
			}
			else if (text.StartsWith("."))
			{
				text = text.Substring(1);
			}
			vcxPropertyGroup.Props.AddItem("veTargetExt", text);
			vcxPropertyGroup.Props.AddItem("veTargetType", (Target.TargetOption.TargetCommonOption.CreateLib == 0) ? "App" : "Lib");
			vcxPropertyGroup.Props.AddItem("veDestDir", Target.TargetOption.TargetCommonOption.OutputDirectory.TrimEnd('\\', '/'));
			vcxPropertyGroup.Props.AddItem("veObjDir", Target.TargetOption.TargetCommonOption.OutputDirectory.TrimEnd('\\', '/'));
			vcxPropertyGroup.Props.AddItem("veListDir", Target.TargetOption.TargetCommonOption.ListingPath.TrimEnd('\\', '/'));
			vcxPropertyGroup.Props.AddItem("veCPUType", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsCpuType);
			vcxPropertyGroup.Props.AddItem("veStdIncDir", UvToVcxPickStdIncDir(Target, thePack));
			vcxPropertyGroup.Props.AddItem("veCommonIncDir", UvToVcxPickCommonIncDir(Target, thePack));
			vcxPropertyGroup.Props.AddItem("veCommonMacros", UvToVcxPickCommonMacros(Target, thePack));
			theVCX.PropertyGroups.Add(vcxPropertyGroup);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static void UvToVcxFillCProps(UvProject.UvArmAds ArmAds, VcxProject.VcxProps Props, string theCond)
	{
		if (ArmAds.Cads != null)
		{
			string text = AdjustMacrosForVcx(ArmAds.Cads.VariousControls.Define);
			if (text == string.Empty)
			{
				text = null;
			}
			Props.AddItem("veCCMacros", text, VcxProject.AttrCond, theCond);
			text = ((ArmAds.Cads.VariousControls.IncludePath == string.Empty) ? null : ArmAds.Cads.VariousControls.IncludePath);
			Props.AddItem("veCCIncDir", text, VcxProject.AttrCond, theCond);
			if (ArmAds.Cads.interw != 2)
			{
				Props.AddItem("veCCInterwork", ArmAds.Cads.interw != 0, VcxProject.AttrCond, theCond);
			}
			Props.AddItem("veCCOptimize", ArmAds.Cads.Optim - 1, VcxProject.AttrCond, theCond);
			if (ArmAds.Cads.oTime != 2)
			{
				Props.AddItem("veCCOptType", (ArmAds.Cads.oTime != 0) ? 1 : 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.useXO != 2)
			{
				Props.AddItem("veCCExecOnly", ArmAds.Cads.useXO != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.SplitLS != 2)
			{
				Props.AddItem("veCCSplitLdm", ArmAds.Cads.SplitLS != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.OneElfS != 2)
			{
				Props.AddItem("veCCSplitSec", ArmAds.Cads.OneElfS != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.Strict != 2)
			{
				Props.AddItem("veCCStrict", ArmAds.Cads.Strict != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.uC99 != 2)
			{
				Props.AddItem("veCC99", ArmAds.Cads.uC99 != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.EnumInt != 2)
			{
				Props.AddItem("veCCEnumAsInt", ArmAds.Cads.EnumInt != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.PlainCh != 2)
			{
				Props.AddItem("veCCSignedChar", ArmAds.Cads.PlainCh != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.Ropi != 2)
			{
				Props.AddItem("veCCRopi", ArmAds.Cads.Ropi != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.Rwpi != 2)
			{
				Props.AddItem("veCCRwpi", ArmAds.Cads.Rwpi != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.wLevel != 2)
			{
				Props.AddItem("veCCWarn", (ArmAds.Cads.wLevel == 1) ? "0" : "3", VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Cads.uThumb != 2)
			{
				Props.AddItem("veCCThumb", ArmAds.Cads.uThumb != 0, VcxProject.AttrCond, theCond);
			}
			text = ((ArmAds.Cads.VariousControls.MiscControls == string.Empty) ? null : ArmAds.Cads.VariousControls.MiscControls);
			Props.AddItem("veCCMisc", text, VcxProject.AttrCond, theCond);
		}
		if (ArmAds.ArmAdsMisc != null)
		{
			Props.AddItem("veCCList", ArmAds.ArmAdsMisc.RvctClst != 0, VcxProject.AttrCond, theCond);
		}
	}

	private bool UvToVcxCCProps(UvProject.UvTarget Target, string theCond)
	{
		try
		{
			VcxProject.VcxPropertyGroup vcxPropertyGroup = new VcxProject.VcxPropertyGroup();
			vcxPropertyGroup.Condition = theCond;
			vcxPropertyGroup.Label = "arm.ve.props.cc";
			UvToVcxFillCProps(Target.TargetOption.TargetArmAds, vcxPropertyGroup.Props, null);
			vcxPropertyGroup.Props.AddItem("veCCDbgInfo", Target.TargetOption.TargetCommonOption.DebugInformation != 0);
			theVCX.PropertyGroups.Add(vcxPropertyGroup);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static void UvToVcxFillAsmProps(UvProject.UvArmAds ArmAds, VcxProject.VcxProps Props, string theCond)
	{
		if (ArmAds.Aads != null)
		{
			string text = AdjustMacrosForVcx(ArmAds.Aads.VariousControls.Define);
			if (text == string.Empty)
			{
				text = null;
			}
			Props.AddItem("veASMacros", text, VcxProject.AttrCond, theCond);
			text = ((ArmAds.Aads.VariousControls.IncludePath == string.Empty) ? null : ArmAds.Aads.VariousControls.IncludePath);
			Props.AddItem("veASIncDir", text, VcxProject.AttrCond, theCond);
			if (ArmAds.Aads.NoWarn != 2)
			{
				Props.AddItem("veASWarn", (ArmAds.Aads.NoWarn == 0) ? "3" : "0", VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Aads.interw != 2)
			{
				Props.AddItem("veASInterwork", ArmAds.Aads.interw != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Aads.thumb != 2)
			{
				Props.AddItem("veASThumb", ArmAds.Aads.thumb != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Aads.useXO != 2)
			{
				Props.AddItem("veASExecOnly", ArmAds.Aads.useXO != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Aads.SplitLS != 2)
			{
				Props.AddItem("veASSplitLdm", ArmAds.Aads.SplitLS != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Aads.Ropi != 2)
			{
				Props.AddItem("veASRopi", ArmAds.Aads.Ropi != 0, VcxProject.AttrCond, theCond);
			}
			if (ArmAds.Aads.Rwpi != 2)
			{
				Props.AddItem("veASRwpi", ArmAds.Aads.Rwpi != 0, VcxProject.AttrCond, theCond);
			}
			text = ((ArmAds.Aads.VariousControls.MiscControls == string.Empty) ? null : ArmAds.Aads.VariousControls.MiscControls);
			Props.AddItem("veASMisc", text, VcxProject.AttrCond, theCond);
		}
		if (ArmAds.ArmAdsMisc != null)
		{
			Props.AddItem("veASList", ArmAds.ArmAdsMisc.AdsALst != 0, VcxProject.AttrCond, theCond);
			Props.AddItem("veASXref", ArmAds.ArmAdsMisc.AdsACrf != 0, VcxProject.AttrCond, theCond);
		}
	}

	private bool UvToVcxAsmProps(UvProject.UvTarget Target, string theCond)
	{
		try
		{
			VcxProject.VcxPropertyGroup vcxPropertyGroup = new VcxProject.VcxPropertyGroup();
			vcxPropertyGroup.Condition = theCond;
			vcxPropertyGroup.Label = "arm.ve.props.asm";
			UvToVcxFillAsmProps(Target.TargetOption.TargetArmAds, vcxPropertyGroup.Props, null);
			vcxPropertyGroup.Props.AddItem("veASDbgInfo", Target.TargetOption.TargetCommonOption.DebugInformation != 0);
			theVCX.PropertyGroups.Add(vcxPropertyGroup);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private bool UvToVcxLinkProps(UvProject.UvTarget Target, string theCond)
	{
		try
		{
			VcxProject.VcxPropertyGroup vcxPropertyGroup = new VcxProject.VcxPropertyGroup();
			vcxPropertyGroup.Condition = theCond;
			vcxPropertyGroup.Label = "arm.ve.props.link";
			vcxPropertyGroup.Props.AddItem("veLnScatter", Target.TargetOption.TargetArmAds.LDads.ScatterFile);
			vcxPropertyGroup.Props.AddItem("veLnDbgInfo", Target.TargetOption.TargetCommonOption.DebugInformation != 0);
			vcxPropertyGroup.Props.AddItem("veLnNoStdLib", Target.TargetOption.TargetArmAds.LDads.noStLib != 0);
			vcxPropertyGroup.Props.AddItem("veLnStrict", Target.TargetOption.TargetArmAds.LDads.RepFail != 0);
			vcxPropertyGroup.Props.AddItem("veLnMap", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsLmap != 0);
			vcxPropertyGroup.Props.AddItem("veLnXref", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsLsxf != 0);
			vcxPropertyGroup.Props.AddItem("veLnCallgraph", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsLcgr != 0);
			vcxPropertyGroup.Props.AddItem("veLnSymbols", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsLsym != 0);
			vcxPropertyGroup.Props.AddItem("veLnInfoSizes", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsLszi != 0);
			vcxPropertyGroup.Props.AddItem("veLnInfoTotals", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsLtoi != 0);
			vcxPropertyGroup.Props.AddItem("veLnInfoUnused", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsLsun != 0);
			vcxPropertyGroup.Props.AddItem("veLnInfoVeneers", Target.TargetOption.TargetArmAds.ArmAdsMisc.AdsLven != 0);
			vcxPropertyGroup.Props.AddItem("veLnSuppress", Target.TargetOption.TargetArmAds.LDads.DisabledWarnings);
			vcxPropertyGroup.Props.AddItem("veLnMisc", Target.TargetOption.TargetArmAds.LDads.Misc);
			theVCX.PropertyGroups.Add(vcxPropertyGroup);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static string UvToVcxCmdConvert(string theCmd, int IsRun)
	{
		string text = string.Empty;
		if (theCmd != null)
		{
			theCmd = theCmd.Trim();
			if (theCmd != string.Empty)
			{
				if (IsRun == 0)
				{
					text += "*";
				}
				text = text + theCmd + "\r\n";
			}
		}
		return text;
	}

	private bool UvToVcxUserProps(UvProject.UvTarget Target, string theCond)
	{
		try
		{
			VcxProject.VcxPropertyGroup vcxPropertyGroup = new VcxProject.VcxPropertyGroup();
			vcxPropertyGroup.Condition = theCond;
			vcxPropertyGroup.Label = "arm.ve.props.customcmd";
			string text = UvToVcxCmdConvert(Target.TargetOption.TargetCommonOption.BeforeCompile.UserProg1Name, Target.TargetOption.TargetCommonOption.BeforeCompile.RunUserProg1);
			text += UvToVcxCmdConvert(Target.TargetOption.TargetCommonOption.BeforeCompile.UserProg2Name, Target.TargetOption.TargetCommonOption.BeforeCompile.RunUserProg2);
			vcxPropertyGroup.Props.AddItem("veUsrBeforeFileProc", text);
			text = UvToVcxCmdConvert(Target.TargetOption.TargetCommonOption.BeforeMake.UserProg1Name, Target.TargetOption.TargetCommonOption.BeforeMake.RunUserProg1);
			text += UvToVcxCmdConvert(Target.TargetOption.TargetCommonOption.BeforeMake.UserProg2Name, Target.TargetOption.TargetCommonOption.BeforeMake.RunUserProg2);
			vcxPropertyGroup.Props.AddItem("veUsrBeforeBuildProc", text);
			text = UvToVcxCmdConvert(Target.TargetOption.TargetCommonOption.AfterMake.UserProg1Name, Target.TargetOption.TargetCommonOption.AfterMake.RunUserProg1);
			text += UvToVcxCmdConvert(Target.TargetOption.TargetCommonOption.AfterMake.UserProg2Name, Target.TargetOption.TargetCommonOption.AfterMake.RunUserProg2);
			vcxPropertyGroup.Props.AddItem("veUsrAfterBuildProc", text);
			theVCX.PropertyGroups.Add(vcxPropertyGroup);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static void UvToVcxPickCfgName(string TargetName, out string theCfgName, out string thePlatform)
	{
		int num = TargetName.IndexOf('.');
		if (num > 0)
		{
			string text = TargetName.Substring(0, num).ToLower().Trim();
			if (text == "debug")
			{
				theCfgName = "Debug";
				thePlatform = TargetName.Substring(num + 1).Trim();
			}
			else if (text == "release")
			{
				theCfgName = "Release";
				thePlatform = TargetName.Substring(num + 1).Trim();
			}
			else
			{
				theCfgName = "Debug";
				thePlatform = TargetName.Trim();
			}
			if (thePlatform == string.Empty)
			{
				thePlatform = "ARM";
			}
		}
		else
		{
			theCfgName = "Debug";
			thePlatform = TargetName.Trim();
		}
	}

	private VcxProject.VcxFileItem UvToVcxProcFile(UvProject.UvFile theUvFile, UvProject.UvTarget theTarget, VcxProject.VcxFileItem theVcxFile)
	{
		try
		{
			int num = UvFileTypeToVcxIdx(theUvFile.FileType, theUvFile.FilePath);
			VcxProject.VcxFileItem vcxFileItem = theVcxFile;
			if (theVcxFile == null && theVcxFlt.AddFileItem(theUvFile.FilePath, FilterItemType[num]))
			{
				vcxFileItem = theVCX.AddFileItem(theUvFile.FilePath, PrjFileItemType[num]) as VcxProject.VcxFileItem;
			}
			if (vcxFileItem != null && theUvFile.FileOption != null && theUvFile.FileOption.CommonProperty != null)
			{
				UvToVcxPickCfgName(theTarget.TargetName, out var theCfgName, out var thePlatform);
				theCfgName = $"'$(Configuration)|$(Platform)'=='{theCfgName}|{thePlatform}'";
				if (theUvFile.FileOption.CommonProperty.IncludeInBuild == 0)
				{
					vcxFileItem.Metadatas.AddItem("ExcludedFromBuild", "true", VcxProject.AttrCond, theCfgName);
				}
				switch (num)
				{
				case 6:
					vcxFileItem.Metadatas.AddItem("CustomCfg", true, VcxProject.AttrCond, theCfgName);
					vcxFileItem.Metadatas.AddItem("BuildCmd", theUvFile.FileOption.CommonProperty.CustomArgument, VcxProject.AttrCond, theCfgName);
					break;
				case 0:
				case 7:
					vcxFileItem.Metadatas.AddItem("CustomCfg", true, VcxProject.AttrCond, theCfgName);
					UvToVcxFillCProps(theUvFile.FileOption.FileArmAds, vcxFileItem.Metadatas, theCfgName);
					break;
				case 1:
					vcxFileItem.Metadatas.AddItem("CustomCfg", true, VcxProject.AttrCond, theCfgName);
					UvToVcxFillAsmProps(theUvFile.FileOption.FileArmAds, vcxFileItem.Metadatas, theCfgName);
					break;
				}
			}
			theUvFile.IsProcessed = true;
			return vcxFileItem;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private bool UvToVcxProcFileInTarget(int TargetIdx)
	{
		bool flag = true;
		foreach (UvProject.UvGroup group in theUV.Targets[TargetIdx].Groups)
		{
			theVcxFlt.AddFilter(group.GroupName, null);
			foreach (UvProject.UvFile file in group.Files)
			{
				if (!file.IsProcessed)
				{
					VcxProject.VcxFileItem vcxFileItem = UvToVcxProcFile(file, theUV.Targets[TargetIdx], null);
					flag = vcxFileItem != null;
					int TargetIdx2 = TargetIdx + 1;
					UvProject.UvTarget Target;
					UvProject.UvGroup Group;
					UvProject.UvFile theFile;
					while (flag && theUV.FindFile(file.FilePath, ref TargetIdx2, out Target, out Group, out theFile))
					{
						if (!theFile.IsProcessed)
						{
							vcxFileItem = UvToVcxProcFile(theFile, Target, vcxFileItem);
							flag = vcxFileItem != null;
						}
						TargetIdx2++;
					}
				}
				if (!flag)
				{
					break;
				}
			}
			if (!flag)
			{
				return flag;
			}
		}
		return flag;
	}

	private bool UvToVcxProcFileInRTE()
	{
		try
		{
			bool result = true;
			foreach (UvProject.UvRTEFile file in theUV.RTE.files)
			{
				string theFilter = $"::{file.component.Cclass}\\{file.component.Cgroup}";
				int num = FileTypeToVcxIdx(file.instance);
				if (result = theVcxFlt.AddFileItem(theFilter, file.instance, FilterItemType[num]))
				{
					if (!(result = theVCX.AddFileItem(file.instance, PrjFileItemType[num]) != null))
					{
						return result;
					}
					continue;
				}
				return result;
			}
			return result;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool UvToVcxPrj(string UvPrjFileName)
	{
		bool flag;
		try
		{
			flag = true;
			thePack = null;
			theUV = UvProject.CreateFromFile(UvPrjFileName);
			theVCX = new VcxProject();
			theVcxFlt = new VcxPrjFilter();
			foreach (UvProject.UvTarget target in theUV.Targets)
			{
				VcxProject.VcxProjectConfiguration vcxProjectConfiguration = new VcxProject.VcxProjectConfiguration();
				UvToVcxPickCfgName(target.TargetName, out vcxProjectConfiguration.Configuration, out vcxProjectConfiguration.Platform);
				vcxProjectConfiguration.Include = $"{vcxProjectConfiguration.Configuration}|{vcxProjectConfiguration.Platform}";
				theVCX.PrjCfgs.Add(vcxProjectConfiguration);
			}
			foreach (UvProject.UvTarget target2 in theUV.Targets)
			{
				UvToVcxPickCfgName(target2.TargetName, out var theCfgName, out var thePlatform);
				theCfgName = $"'$(Configuration)|$(Platform)'=='{theCfgName}|{thePlatform}'";
				if (!(flag = UvToVcxGeneralProps(target2, theCfgName)) || !(flag = UvToVcxUVisionProps(target2, theCfgName)) || !(flag = UvToVcxCCProps(target2, theCfgName)) || !(flag = UvToVcxAsmProps(target2, theCfgName)) || !(flag = UvToVcxLinkProps(target2, theCfgName)) || !(flag = UvToVcxUserProps(target2, theCfgName)))
				{
					break;
				}
			}
			if (flag)
			{
				for (int i = 0; i < theUV.Targets.Count; i++)
				{
					flag = UvToVcxProcFileInTarget(i);
					if (!flag)
					{
						break;
					}
				}
			}
			if (flag)
			{
				flag = UvToVcxProcFileInRTE();
			}
		}
		catch (Exception ex)
		{
			ShowError("转换时出现异常：{0}", ex.Message);
			flag = false;
		}
		thePack = null;
		return flag;
	}
}

// VisualEmbed.ProjectSupport.UvPack
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VisualEmbed.ProjectSupport;

[XmlType("package")]
public class UvPack : BaseProject
{
	[XmlType("release")]
	public struct release
	{
		[XmlAttribute]
		public string version;

		[XmlAttribute]
		public string date;

		[XmlText]
		public string description;
	}

	[XmlType("keyword")]
	public struct keyword
	{
		[XmlText]
		public string word;
	}

	[XmlType("processor")]
	public struct ProcessorType
	{
		[XmlAttribute]
		public string Pname;

		[XmlAttribute]
		public string Dcore;

		[XmlAttribute]
		public string Dfpu;

		[XmlAttribute]
		public string Dmpu;

		[XmlAttribute]
		public string Dendian;

		[XmlAttribute]
		public uint Dclock;

		[XmlAttribute]
		public string DcoreVersion;
	}

	[XmlType("book")]
	public struct book
	{
		[XmlAttribute]
		public string Pname;

		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public string title;
	}

	[XmlType("description")]
	public struct DescriptionType
	{
		[XmlAttribute]
		public string Pname;

		[XmlText]
		public string value;
	}

	[XmlType("compile")]
	public class CompileType
	{
		[XmlAttribute]
		public string Pname;

		[XmlAttribute]
		public string header;

		[XmlAttribute]
		public string define;
	}

	public class DataPatchType
	{
		[XmlAttribute]
		public string type;

		[XmlAttribute]
		public string address;

		[XmlAttribute]
		public uint __dp;

		[XmlAttribute]
		public uint __ap;

		[XmlAttribute]
		public string value;

		[XmlAttribute]
		public string mask;

		[XmlAttribute]
		public string info;

		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();
	}

	public class JtagType
	{
		[XmlAttribute]
		public string tapindex;

		[XmlAttribute]
		public string idcode;

		[XmlAttribute]
		public string targetsel;

		[XmlAttribute]
		public uint irlen;

		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();
	}

	public class SwdType
	{
		[XmlAttribute]
		public string idcode;

		[XmlAttribute]
		public string targetsel;

		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();
	}

	[XmlType("debugport")]
	public class DebugPortType
	{
		[XmlElement]
		public JtagType jtag;

		[XmlElement]
		public SwdType swd;

		[XmlAttribute]
		public uint __dp;

		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();
	}

	[XmlType("environment")]
	public class EnvironmentType
	{
		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public string Pname;

		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();
	}

	public class SerialWireType
	{
		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();

		[XmlText]
		public string _value;
	}

	public class TracePortType
	{
		[XmlAttribute]
		public string width;

		[XmlAnyAttribute]
		public List<XmlNode> skip = new List<XmlNode>();

		[XmlText]
		public string _value;
	}

	public class TraceBufferType
	{
		[XmlAttribute]
		public string start;

		[XmlAttribute]
		public string size;

		[XmlAnyAttribute]
		public List<XmlNode> skip = new List<XmlNode>();

		[XmlText]
		public string _value;
	}

	[XmlType("trace")]
	public struct TraceType
	{
		[XmlElement]
		public SerialWireType serialwire;

		[XmlElement]
		public TracePortType traceport;

		[XmlElement]
		public TraceBufferType tracebuffer;

		[XmlAttribute]
		public string Pname;

		[XmlAnyAttribute]
		public List<XmlNode> lax;
	}

	[XmlType("debugvars")]
	public class DebugVarsType
	{
		[XmlAttribute]
		public string configfile;

		[XmlAttribute]
		public string version;

		[XmlAttribute]
		public string Pname;

		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();

		[XmlText]
		public string _value;
	}

	[XmlType("debug")]
	public class DebugType
	{
		[XmlElement]
		public DataPatchType datapatch;

		[XmlAttribute]
		public uint __dp;

		[XmlAttribute]
		public uint __ap;

		[XmlAttribute]
		public string svd;

		[XmlAttribute]
		public string Pname;

		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();
	}

	[XmlType("memory")]
	public struct MemoryType
	{
		[XmlAttribute]
		public string Pname;

		[XmlAttribute]
		public string id;

		[XmlAttribute]
		public string start;

		[XmlAttribute]
		public string size;

		[XmlAttribute]
		public uint init;

		[XmlAttribute("default")]
		public uint _default;

		[XmlAttribute]
		public uint startup;
	}

	[XmlType("algorithm")]
	public struct AlgorithmType
	{
		[XmlAttribute]
		public string Pname;

		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public string start;

		[XmlAttribute]
		public string size;

		[XmlAttribute]
		public string RAMstart;

		[XmlAttribute]
		public string RAMsize;

		[XmlAttribute("default")]
		public uint _default;
	}

	public class DebugConfigType
	{
		[XmlAttribute("default")]
		public string _default;

		[XmlAttribute]
		public uint clock;

		[XmlAttribute]
		public uint swj;

		[XmlAnyAttribute]
		public List<XmlNode> lax = new List<XmlNode>();
	}

	[XmlType("feature")]
	public struct DeviceFeatureType
	{
		[XmlAttribute]
		public string Pname;

		[XmlAttribute]
		public string type;

		[XmlAttribute]
		public decimal n;

		[XmlAttribute]
		public decimal m;

		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public int count;
	}

	public abstract class BaseInfoSet
	{
		[XmlElement]
		public ProcessorType processor;

		[XmlElement]
		public DebugConfigType debugconfig;

		[XmlElement(Type = typeof(CompileType))]
		[XmlElement(Type = typeof(MemoryType))]
		[XmlElement(Type = typeof(AlgorithmType))]
		[XmlElement(Type = typeof(DescriptionType))]
		[XmlElement(Type = typeof(DeviceFeatureType))]
		[XmlElement(Type = typeof(EnvironmentType))]
		[XmlElement(Type = typeof(DebugPortType))]
		[XmlElement(Type = typeof(DebugType))]
		[XmlElement(Type = typeof(TraceType))]
		[XmlElement(Type = typeof(DebugVarsType))]
		public List<object> props;

		[XmlIgnore]
		public BaseInfoSet _parent;
	}

	[XmlType("device")]
	public class DeviceType : BaseInfoSet
	{
		[XmlAttribute]
		public string Dname;
	}

	[XmlType("subFamily")]
	public class subFamilyType : BaseInfoSet
	{
		[XmlElement("device")]
		public List<DeviceType> _devices;

		[XmlAttribute]
		public string DsubFamily;
	}

	[XmlType("family")]
	public class FamilyType : BaseInfoSet
	{
		[XmlElement("device")]
		public List<DeviceType> _devices;

		[XmlElement("subFamily")]
		public List<subFamilyType> _subFamilys;

		[XmlAttribute]
		public string Dfamily;

		[XmlAttribute]
		public string Dvendor;
	}

	[XmlElement]
	public string vendor;

	[XmlElement]
	public string url;

	[XmlElement]
	public string name;

	[XmlElement]
	public string description;

	[XmlArray]
	public List<release> releases = new List<release>();

	[XmlArray]
	public List<keyword> keywords = new List<keyword>();

	[XmlArray]
	public List<FamilyType> devices = new List<FamilyType>();

	[XmlIgnore]
	private string _PackFile;

	[XmlIgnore]
	public string PackFile => _PackFile;

	[XmlIgnore]
	public string PackRelativeRoot
	{
		get
		{
			string text = IDEPath.MdkPath.ToLower();
			if (_PackFile.ToLower().StartsWith(text))
			{
				return Path.GetDirectoryName(_PackFile.Substring(text.Length + 1));
			}
			return Path.GetDirectoryName(_PackFile);
		}
	}

	private new static object Create(Type type, Stream theStream)
	{
		return null;
	}

	private new static object Create(Type type, string theXml)
	{
		return null;
	}

	private void BuildDeviceLookBackInfo(BaseInfoSet theParent, List<DeviceType> DeviceSet)
	{
		if (DeviceSet == null)
		{
			return;
		}
		foreach (DeviceType item in DeviceSet)
		{
			item._parent = theParent;
		}
	}

	private void BuildSubFamilyLookBackInfo(BaseInfoSet theParent, List<subFamilyType> subFamilySet)
	{
		if (subFamilySet == null)
		{
			return;
		}
		foreach (subFamilyType item in subFamilySet)
		{
			item._parent = theParent;
			BuildDeviceLookBackInfo(item, item._devices);
		}
	}

	private void BuildLookBackInfo()
	{
		try
		{
			foreach (FamilyType device in devices)
			{
				device._parent = null;
				BuildDeviceLookBackInfo(device, device._devices);
			}
		}
		catch (Exception)
		{
		}
	}

	public static UvPack CreateFromFile(string theUvPackFile)
	{
		UvPack uvPack = (UvPack)BaseProject.CreateFromFile(typeof(UvPack), theUvPackFile);
		if (uvPack != null)
		{
			uvPack._PackFile = theUvPackFile;
			uvPack.BuildLookBackInfo();
		}
		return uvPack;
	}

	public static UvPack CreateFromID(string theUvPackID)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		string text3 = string.Empty;
		int num = theUvPackID.IndexOf('.');
		if (num > 0)
		{
			text = theUvPackID.Substring(0, num);
			int num2 = theUvPackID.IndexOf('.', num + 1);
			if (num2 > 0)
			{
				text2 = theUvPackID.Substring(num + 1, num2 - num - 1);
				text3 = theUvPackID.Substring(num2 + 1);
			}
		}
		return CreateFromFile(string.Format("{0}\\Pack\\{1}\\{2}\\{3}\\{1}.{2}.pdsc", IDEPath.MdkPath, text, text2, text3));
	}

	public DeviceType FindDevice(string DeviceName, List<DeviceType> DeviceSet)
	{
		DeviceType result = null;
		if (DeviceSet != null)
		{
			foreach (DeviceType item in DeviceSet)
			{
				if (DeviceName.Equals(item.Dname, StringComparison.CurrentCultureIgnoreCase))
				{
					return item;
				}
			}
			return result;
		}
		return result;
	}

	public DeviceType FindDevice(string DeviceName)
	{
		DeviceType deviceType = null;
		try
		{
			foreach (FamilyType device in devices)
			{
				deviceType = FindDevice(DeviceName, device._devices);
				if (deviceType == null)
				{
					foreach (subFamilyType subFamily in device._subFamilys)
					{
						deviceType = FindDevice(DeviceName, subFamily._devices);
						if (deviceType != null)
						{
							break;
						}
					}
					if (deviceType != null)
					{
						return deviceType;
					}
					continue;
				}
				return deviceType;
			}
			return deviceType;
		}
		catch (Exception)
		{
			return deviceType;
		}
	}
}

// VisualEmbed.ProjectSupport.UvProject
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using VisualEmbed.ProjectSupport;

[XmlType("Project")]
public sealed class UvProject : BaseProject
{
	public sealed class UvUsrCmd
	{
		[XmlElement]
		public int RunUserProg1;

		[XmlElement]
		public int RunUserProg2;

		[XmlElement]
		public string UserProg1Name;

		[XmlElement]
		public string UserProg2Name;

		[XmlElement]
		public int UserProg1Dos16Mode;

		[XmlElement]
		public int UserProg2Dos16Mode;

		[XmlElement]
		public int nStopA1X;

		[XmlElement]
		public int nStopA2X;
	}

	[XmlType("TargetStatus")]
	public sealed class UvTargetStatus
	{
		[XmlElement]
		public int Error;

		[XmlElement]
		public int ExitCodeStop;

		[XmlElement]
		public int ButtonStop;

		[XmlElement]
		public int NotGenerated;

		[XmlElement]
		public int InvalidFlash = 1;
	}

	[XmlType("TargetCommonOption")]
	public sealed class UvTargetCommonOption
	{
		[XmlElement]
		public string Device;

		[XmlElement]
		public string Vendor;

		[XmlElement]
		public string PackID;

		[XmlElement]
		public string PackURL;

		[XmlElement]
		public string Cpu;

		[XmlElement]
		public string FlashUtilSpec;

		[XmlElement]
		public string StartupFile;

		[XmlElement]
		public string FlashDriverDll;

		[XmlElement]
		public string DeviceId = "0";

		[XmlElement]
		public string RegisterFile;

		[XmlElement]
		public string MemoryEnv;

		[XmlElement]
		public string Cmp;

		[XmlElement]
		public string Asm;

		[XmlElement]
		public string Linker;

		[XmlElement]
		public string OHString;

		[XmlElement]
		public string InfinionOptionDll;

		[XmlElement]
		public string SLE66CMisc;

		[XmlElement]
		public string SLE66AMisc;

		[XmlElement]
		public string SLE66LinkerMisc;

		[XmlElement]
		public string SFDFile;

		[XmlElement]
		public int bCustSvd;

		[XmlElement]
		public int UseEnv;

		[XmlElement]
		public string BinPath;

		[XmlElement]
		public string IncludePath;

		[XmlElement]
		public string LibPath;

		[XmlElement]
		public string RegisterFilePath;

		[XmlElement]
		public string DBRegisterFilePath;

		public UvTargetStatus TargetStatus = new UvTargetStatus();

		[XmlElement]
		public string OutputDirectory;

		[XmlElement]
		public string OutputName;

		[XmlElement]
		public int CreateExecutable = 1;

		[XmlElement]
		public int CreateLib;

		[XmlElement]
		public int CreateHexFile;

		[XmlElement]
		public int DebugInformation = 1;

		[XmlElement]
		public int BrowseInformation;

		[XmlElement]
		public string ListingPath;

		[XmlElement]
		public int HexFormatSelection = 1;

		[XmlElement]
		public int Merge32K;

		[XmlElement]
		public int CreateBatchFile;

		[XmlElement]
		public UvUsrCmd BeforeCompile = new UvUsrCmd();

		[XmlElement]
		public UvUsrCmd AfterCompile = new UvUsrCmd();

		[XmlElement]
		public UvUsrCmd BeforeMake = new UvUsrCmd();

		[XmlElement]
		public UvUsrCmd AfterMake = new UvUsrCmd();

		[XmlElement]
		public int SelectedForBatchBuild;

		[XmlElement]
		public string SVCSIdString;
	}

	[XmlType("CommonProperty")]
	public sealed class UvCommonProperty
	{
		[XmlElement]
		public int UseCPPCompiler;

		[XmlElement]
		public int RVCTCodeConst;

		[XmlElement]
		public int RVCTZI;

		[XmlElement]
		public int RVCTOtherData;

		[XmlElement]
		public int ModuleSelection;

		[XmlElement]
		public int IncludeInBuild = 1;

		[XmlElement]
		public int AlwaysBuild;

		[XmlElement]
		public int GenerateAssemblyFile;

		[XmlElement]
		public int AssembleAssemblyFile;

		[XmlElement]
		public int PublicsOnly;

		[XmlElement]
		public int StopOnExitCode = 3;

		[XmlElement]
		public string CustomArgument;

		[XmlElement]
		public string IncludeLibraryModules;

		[XmlElement]
		public int ComprImg = 1;
	}

	[XmlType("DllOption")]
	public sealed class UvDllOption
	{
		[XmlElement]
		public string SimDllName;

		[XmlElement]
		public string SimDllArguments;

		[XmlElement]
		public string SimDlgDll;

		[XmlElement]
		public string SimDlgDllArguments;

		[XmlElement]
		public string TargetDllName;

		[XmlElement]
		public string TargetDllArguments;

		[XmlElement]
		public string TargetDlgDll;

		[XmlElement]
		public string TargetDlgDllArguments;
	}

	[XmlType("OPTHX")]
	public sealed class UvDbgOPTHX
	{
		[XmlElement]
		public int HexSelection = 1;

		[XmlElement]
		public int HexRangeLowAddress;

		[XmlElement]
		public int HexRangeHighAddress;

		[XmlElement]
		public int HexOffset;

		[XmlElement]
		public int Oh166RecLen = 16;
	}

	[XmlType("Simulator")]
	public sealed class UvDbgSimulator
	{
		[XmlElement]
		public int UseSimulator = 1;

		[XmlElement]
		public int LoadApplicationAtStartup = 1;

		[XmlElement]
		public int RunToMain = 1;

		[XmlElement]
		public int RestoreBreakpoints = 1;

		[XmlElement]
		public int RestoreWatchpoints = 1;

		[XmlElement]
		public int RestoreMemoryDisplay = 1;

		[XmlElement]
		public int RestoreFunctions = 1;

		[XmlElement]
		public int RestoreToolbox = 1;

		[XmlElement]
		public int LimitSpeedToRealTime = 1;

		[XmlElement]
		public int RestoreSysVw = 1;
	}

	public sealed class UvDbgTarget
	{
		[XmlElement]
		public int UseTarget;

		[XmlElement]
		public int LoadApplicationAtStartup = 1;

		[XmlElement]
		public int RunToMain = 1;

		[XmlElement]
		public int RestoreBreakpoints = 1;

		[XmlElement]
		public int RestoreWatchpoints = 1;

		[XmlElement]
		public int RestoreMemoryDisplay = 1;

		[XmlElement]
		public int RestoreFunctions = 1;

		[XmlElement]
		public int RestoreToolbox = 1;

		[XmlElement]
		public int LimitSpeedToRealTime = 1;

		[XmlElement]
		public int RestoreSysVw = 1;
	}

	[XmlType("SimDlls")]
	public sealed class UvSimDlls
	{
		[XmlElement]
		public string CpuDll;

		[XmlElement]
		public string CpuDllArguments;

		[XmlElement]
		public string PeripheralDll;

		[XmlElement]
		public string PeripheralDllArguments;

		[XmlElement]
		public string InitializationFile;
	}

	[XmlType("TargetDlls")]
	public sealed class UvTargetDlls
	{
		[XmlElement]
		public string CpuDll;

		[XmlElement]
		public string CpuDllArguments;

		[XmlElement]
		public string PeripheralDll;

		[XmlElement]
		public string PeripheralDllArguments;

		[XmlElement]
		public string InitializationFile;

		[XmlElement]
		public string Driver;
	}

	[XmlType("DebugOption")]
	public sealed class UvDebugOption
	{
		public UvDbgOPTHX OPTHX = new UvDbgOPTHX();

		public UvDbgSimulator Simulator = new UvDbgSimulator();

		[XmlElement("Target")]
		public UvDbgTarget Target = new UvDbgTarget();

		[XmlElement]
		public int RunDebugAfterBuild;

		[XmlElement]
		public int TargetSelection;

		public UvSimDlls SimDlls = new UvSimDlls();

		public UvTargetDlls TargetDlls = new UvTargetDlls();
	}

	[XmlType("Flash1")]
	public sealed class UvUtlFlash1
	{
		[XmlElement]
		public int UseTargetDll = 1;

		[XmlElement]
		public int UseExternalTool;

		[XmlElement]
		public int RunIndependent;

		[XmlElement]
		public int UpdateFlashBeforeDebugging = 1;

		[XmlElement]
		public int Capability = 1;

		[XmlElement]
		public int DriverSelection = 4096;
	}

	[XmlType("Utilities")]
	public sealed class UvUtilities
	{
		public UvUtlFlash1 Flash1 = new UvUtlFlash1();

		[XmlElement]
		public int bUseTDR = 1;

		[XmlElement]
		public string Flash2;

		[XmlElement]
		public string Flash3;

		[XmlElement]
		public string Flash4;

		[XmlElement]
		public string pFcarmOut;

		[XmlElement]
		public string pFcarmGrp;

		[XmlElement]
		public string pFcArmRoot;

		[XmlElement]
		public string FcArmLst;
	}

	public sealed class UvMemDec
	{
		[XmlElement]
		public int Type;

		[XmlElement]
		public string StartAddress = "0x0";

		[XmlElement]
		public string Size = "0x0";
	}

	[XmlType("OnChipMemories")]
	public sealed class UvOnChipMemories
	{
		[XmlElement]
		public UvMemDec Ocm1 = new UvMemDec();

		[XmlElement]
		public UvMemDec Ocm2 = new UvMemDec();

		[XmlElement]
		public UvMemDec Ocm3 = new UvMemDec();

		[XmlElement]
		public UvMemDec Ocm4 = new UvMemDec();

		[XmlElement]
		public UvMemDec Ocm5 = new UvMemDec();

		[XmlElement]
		public UvMemDec Ocm6 = new UvMemDec();

		[XmlElement]
		public UvMemDec IRAM = new UvMemDec();

		[XmlElement]
		public UvMemDec IROM = new UvMemDec();

		[XmlElement]
		public UvMemDec XRAM = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT1 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT2 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT3 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT4 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT5 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT6 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT7 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT8 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT9 = new UvMemDec();

		[XmlElement]
		public UvMemDec OCR_RVCT10 = new UvMemDec();
	}

	[XmlType("ArmAdsMisc")]
	public sealed class UvArmAdsMisc
	{
		[XmlElement]
		public int GenerateListings;

		[XmlElement]
		public int asHll = 1;

		[XmlElement]
		public int asAsm = 1;

		[XmlElement]
		public int asMacX = 1;

		[XmlElement]
		public int asSyms = 1;

		[XmlElement]
		public int asFals = 1;

		[XmlElement]
		public int asDbgD = 1;

		[XmlElement]
		public int asForm = 1;

		[XmlElement]
		public int ldLst;

		[XmlElement]
		public int ldmm = 1;

		[XmlElement]
		public int ldXref = 1;

		[XmlElement]
		public int BigEnd;

		[XmlElement]
		public int AdsALst;

		[XmlElement]
		public int AdsACrf = 1;

		[XmlElement]
		public int AdsANop;

		[XmlElement]
		public int AdsANot;

		[XmlElement]
		public int AdsLLst;

		[XmlElement]
		public int AdsLmap = 1;

		[XmlElement]
		public int AdsLcgr = 1;

		[XmlElement]
		public int AdsLsym = 1;

		[XmlElement]
		public int AdsLszi = 1;

		[XmlElement]
		public int AdsLtoi = 1;

		[XmlElement]
		public int AdsLsun = 1;

		[XmlElement]
		public int AdsLven = 1;

		[XmlElement]
		public int AdsLsxf = 1;

		[XmlElement]
		public int RvctClst = 1;

		[XmlElement]
		public int GenPPlst;

		[XmlElement]
		public string AdsCpuType;

		[XmlElement]
		public string RvctDeviceName;

		[XmlElement]
		public int mOS;

		[XmlElement]
		public int uocRom;

		[XmlElement]
		public int uocRam;

		[XmlElement]
		public int hadIROM = 1;

		[XmlElement]
		public int hadIRAM = 1;

		[XmlElement]
		public int hadXRAM;

		[XmlElement]
		public int uocXRam;

		[XmlElement]
		public int RvdsVP;

		[XmlElement]
		public int hadIRAM2;

		[XmlElement]
		public int hadIROM2;

		[XmlElement]
		public int StupSel = 8;

		[XmlElement]
		public int useUlib;

		[XmlElement]
		public int EndSel;

		[XmlElement]
		public int uLtcg;

		[XmlElement]
		public int nSecure;

		[XmlElement]
		public int RoSelD = 3;

		[XmlElement]
		public int RwSelD = 3;

		[XmlElement]
		public int CodeSel;

		[XmlElement]
		public int OptFeed;

		[XmlElement]
		public int NoZi1;

		[XmlElement]
		public int NoZi2;

		[XmlElement]
		public int NoZi3;

		[XmlElement]
		public int NoZi4;

		[XmlElement]
		public int NoZi5;

		[XmlElement]
		public int Ro1Chk;

		[XmlElement]
		public int Ro2Chk;

		[XmlElement]
		public int Ro3Chk;

		[XmlElement]
		public int Ir1Chk = 1;

		[XmlElement]
		public int Ir2Chk;

		[XmlElement]
		public int Ra1Chk;

		[XmlElement]
		public int Ra2Chk;

		[XmlElement]
		public int Ra3Chk;

		[XmlElement]
		public int Im1Chk = 1;

		[XmlElement]
		public int Im2Chk;

		public UvOnChipMemories OnChipMemories = new UvOnChipMemories();

		[XmlElement]
		public string RvctStartVector;
	}

	[XmlType("VariousControls")]
	public sealed class UvVariousControls
	{
		[XmlElement("MiscControls")]
		public string MiscControls = string.Empty;

		[XmlElement("Define")]
		public string Define = string.Empty;

		[XmlElement("Undefine")]
		public string Undefine = string.Empty;

		[XmlElement("IncludePath")]
		public string IncludePath = string.Empty;
	}

	[XmlType("Cads")]
	public sealed class UvCads
	{
		[XmlElement]
		public int interw = 1;

		[XmlElement]
		public int Optim = 1;

		[XmlElement]
		public int oTime;

		[XmlElement]
		public int SplitLS;

		[XmlElement]
		public int OneElfS = 1;

		[XmlElement]
		public int Strict;

		[XmlElement]
		public int EnumInt;

		[XmlElement]
		public int PlainCh;

		[XmlElement]
		public int Ropi;

		[XmlElement]
		public int Rwpi;

		[XmlElement]
		public int wLevel = 2;

		[XmlElement]
		public int uThumb;

		[XmlElement]
		public int uSurpInc;

		[XmlElement]
		public int uC99;

		[XmlElement]
		public int useXO;

		[XmlElement]
		public int v6Lang;

		[XmlElement]
		public int v6LangP;

		[XmlElement]
		public int vShortEn;

		[XmlElement]
		public int vShortWch;

		[XmlElement]
		public UvVariousControls VariousControls = new UvVariousControls();
	}

	[XmlType("Aads")]
	public sealed class UvAads
	{
		[XmlElement]
		public int interw = 1;

		[XmlElement]
		public int Ropi;

		[XmlElement]
		public int Rwpi;

		[XmlElement]
		public int thumb;

		[XmlElement]
		public int SplitLS;

		[XmlElement]
		public int SwStkChk;

		[XmlElement]
		public int NoWarn;

		[XmlElement]
		public int uSurpInc;

		[XmlElement]
		public int useXO;

		[XmlElement]
		public UvVariousControls VariousControls = new UvVariousControls();
	}

	[XmlType("LDads")]
	public sealed class UvLDads
	{
		[XmlElement]
		public int umfTarg = 1;

		[XmlElement]
		public int Ropi;

		[XmlElement]
		public int Rwpi;

		[XmlElement]
		public int noStLib;

		[XmlElement]
		public int RepFail = 1;

		[XmlElement]
		public int useFile;

		[XmlElement]
		public string TextAddressRange;

		[XmlElement]
		public string DataAddressRange;

		[XmlElement]
		public string pXoBase;

		[XmlElement]
		public string ScatterFile;

		[XmlElement]
		public string IncludeLibs;

		[XmlElement]
		public string IncludeLibsPath;

		[XmlElement]
		public string Misc;

		[XmlElement]
		public string LinkerInputFile;

		[XmlElement]
		public string DisabledWarnings;
	}

	public struct UvArmAds
	{
		public UvArmAdsMisc ArmAdsMisc;

		public UvCads Cads;

		public UvAads Aads;

		public UvLDads LDads;
	}

	[XmlType("TargetOption")]
	public sealed class UvTargetOption
	{
		public UvTargetCommonOption TargetCommonOption = new UvTargetCommonOption();

		public UvCommonProperty CommonProperty = new UvCommonProperty();

		public UvDllOption DllOption = new UvDllOption();

		public UvDebugOption DebugOption = new UvDebugOption();

		public UvUtilities Utilities = new UvUtilities();

		[XmlElement("TargetArmAds")]
		public UvArmAds TargetArmAds;
	}

	[XmlType("FileOption")]
	public class UvFileOption
	{
		public UvCommonProperty CommonProperty;

		[XmlElement("FileArmAds")]
		public UvArmAds FileArmAds;
	}

	[XmlType("File")]
	public sealed class UvFile
	{
		[XmlElement]
		public string FileName;

		[XmlElement]
		public int FileType;

		[XmlElement]
		public string FilePath;

		public UvFileOption FileOption;

		[XmlIgnore]
		public bool IsProcessed;
	}

	[XmlType("Files")]
	public sealed class UvFileList : List<UvFile>
	{
	}

	[XmlType("Group")]
	public sealed class UvGroup
	{
		[XmlElement]
		public string GroupName;

		public UvFileList Files = new UvFileList();
	}

	[XmlType("Groups")]
	public sealed class UvGroupList : List<UvGroup>
	{
	}

	[XmlType("Target")]
	public sealed class UvTarget
	{
		[XmlElement]
		public string TargetName;

		[XmlElement]
		public string ToolsetNumber;

		[XmlElement]
		public string ToolsetName;

		[XmlElement]
		public string pCCUsed;

		public UvTargetOption TargetOption = new UvTargetOption();

		public UvGroupList Groups = new UvGroupList();
	}

	[XmlType("Targets")]
	public sealed class UvTargetList : List<UvTarget>
	{
	}

	[XmlType("package")]
	public sealed class UvRTEPackage
	{
		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public string schemaVersion;

		[XmlAttribute]
		public string url;

		[XmlAttribute]
		public string vendor;

		[XmlAttribute]
		public string version;
	}

	[XmlType("targetInfo")]
	public sealed class UvRTETargetInfo
	{
		[XmlAttribute("name")]
		public string name;
	}

	[XmlType("targetInfos")]
	public sealed class UvRTETargetInfoList : List<UvRTETargetInfo>
	{
	}

	[XmlType("component")]
	public sealed class UvRTEComponent
	{
		[XmlAttribute]
		public string Cclass;

		[XmlAttribute]
		public string Cgroup;

		[XmlAttribute]
		public string Cvendor;

		[XmlAttribute]
		public string Cversion;

		[XmlAttribute]
		public string condition;

		[XmlElement]
		public UvRTEPackage package = new UvRTEPackage();

		public UvRTETargetInfoList targetInfos = new UvRTETargetInfoList();
	}

	[XmlType("components")]
	public sealed class UvRTEComponentList : List<UvRTEComponent>
	{
	}

	[XmlType("file")]
	public sealed class UvRTEFile
	{
		[XmlAttribute]
		public string attr;

		[XmlAttribute]
		public string category;

		[XmlAttribute]
		public string name;

		[XmlElement]
		public string instance;

		public UvRTEComponent component = new UvRTEComponent();

		public UvRTEPackage package = new UvRTEPackage();

		public UvRTETargetInfoList targetInfos = new UvRTETargetInfoList();
	}

	[XmlType("files")]
	public sealed class UvRTEFileList : List<UvRTEFile>
	{
	}

	[XmlType("RTE")]
	public sealed class UvRTE
	{
		public UvRTEComponentList components = new UvRTEComponentList();

		public UvRTEFileList files = new UvRTEFileList();
	}

	[XmlElement]
	public string SchemaVersion;

	[XmlElement]
	public string Header;

	public UvTargetList Targets = new UvTargetList();

	public UvRTE RTE = new UvRTE();

	public static UvProject Create(Stream theStream)
	{
		return (UvProject)BaseProject.Create(typeof(UvProject), theStream);
	}

	public static UvProject Create(string theXml)
	{
		return (UvProject)BaseProject.Create(typeof(UvProject), theXml);
	}

	public static UvProject CreateFromFile(string theUvProjxFile)
	{
		return (UvProject)BaseProject.CreateFromFile(typeof(UvProject), theUvProjxFile);
	}

	public bool FindFile(string FilePath, ref int TargetIdx, out UvTarget Target, out UvGroup Group, out UvFile theFile)
	{
		Target = null;
		Group = null;
		theFile = null;
		try
		{
			if (TargetIdx < 0)
			{
				TargetIdx = 0;
			}
			if (TargetIdx < Targets.Count)
			{
				string text = FilePath.Trim().ToLower();
				while (TargetIdx < Targets.Count)
				{
					foreach (UvGroup group in Targets[TargetIdx].Groups)
					{
						foreach (UvFile file in group.Files)
						{
							if (file.FilePath.Trim().ToLower() == text)
							{
								Target = Targets[TargetIdx];
								Group = group;
								theFile = file;
								return true;
							}
						}
					}
					TargetIdx++;
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	public bool AddTarget(string TargetName, out UvTarget theTarget)
	{
		bool result = false;
		try
		{
			UvTarget uvTarget = new UvTarget();
			uvTarget.TargetName = TargetName;
			Targets.Add(uvTarget);
			theTarget = uvTarget;
			result = true;
			return result;
		}
		catch (Exception)
		{
			theTarget = null;
			return result;
		}
	}

	public bool AddGroup(UvTarget target, string GroupName, out UvGroup theGroup)
	{
		bool result = false;
		try
		{
			if (target != null)
			{
				UvGroup uvGroup = new UvGroup();
				uvGroup.GroupName = GroupName;
				target.Groups.Add(uvGroup);
				theGroup = uvGroup;
				result = true;
				return result;
			}
			theGroup = null;
			return result;
		}
		catch (Exception)
		{
			theGroup = null;
			return result;
		}
	}

	public bool AddFile(UvGroup group, string FilePathName, int FileType, out UvFile theFile)
	{
		bool result = false;
		try
		{
			if (group != null)
			{
				UvFile uvFile = new UvFile();
				uvFile.FileName = Path.GetFileName(FilePathName);
				uvFile.FilePath = FilePathName;
				uvFile.FileType = FileType;
				group.Files.Add(uvFile);
				theFile = uvFile;
				result = true;
				return result;
			}
			theFile = null;
			return result;
		}
		catch (Exception)
		{
			theFile = null;
			return result;
		}
	}
}

// VisualEmbed.ProjectSupport.VcxPrjFilter
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using VisualEmbed.ProjectSupport;

[XmlRoot(Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
[XmlType("Project")]
public class VcxPrjFilter : BaseProject
{
	[XmlType("Filter")]
	public class VcxFilter
	{
		[XmlAttribute]
		public string Include;

		[XmlElement]
		public string UniqueIdentifier;

		[XmlElement]
		public string Extensions;
	}

	public abstract class VcxFltFileItem
	{
		[XmlAttribute]
		public string Include;

		[XmlElement]
		public string Filter;
	}

	[XmlType("Text")]
	public class VcxFltText : VcxFltFileItem
	{
	}

	[XmlType("ClCompile")]
	public class VcxFltCItem : VcxFltFileItem
	{
	}

	[XmlType("ClInclude")]
	public class VcxFltInc : VcxFltFileItem
	{
	}

	[XmlType("AsmItem")]
	public class VcxFltAsmItem : VcxFltFileItem
	{
	}

	[XmlType("Object")]
	public class VcxFltObj : VcxFltFileItem
	{
	}

	[XmlType("Library")]
	public class VcxFltLib : VcxFltFileItem
	{
	}

	[XmlType("None")]
	public class VcxFltNone : VcxFltFileItem
	{
	}

	[XmlType("CustomItem")]
	public class VcxFltCustomItem : VcxFltFileItem
	{
	}

	[XmlAttribute]
	public string ToolsVersion;

	[XmlArrayItem(Type = typeof(VcxFilter))]
	[XmlArrayItem(Type = typeof(VcxFltText))]
	[XmlArrayItem(Type = typeof(VcxFltCItem))]
	[XmlArrayItem(Type = typeof(VcxFltInc))]
	[XmlArrayItem(Type = typeof(VcxFltAsmItem))]
	[XmlArrayItem(Type = typeof(VcxFltObj))]
	[XmlArrayItem(Type = typeof(VcxFltLib))]
	[XmlArrayItem(Type = typeof(VcxFltCustomItem))]
	[XmlArrayItem(Type = typeof(VcxFltNone))]
	public List<object> ItemGroup = new List<object>();

	private string CurFilter;

	public static VcxPrjFilter Create(Stream theStream)
	{
		return (VcxPrjFilter)BaseProject.Create(typeof(VcxPrjFilter), theStream);
	}

	public static VcxPrjFilter Create(string theXml)
	{
		return (VcxPrjFilter)BaseProject.Create(typeof(VcxPrjFilter), theXml);
	}

	public static VcxPrjFilter CreateFromFile(string theFile)
	{
		return (VcxPrjFilter)BaseProject.CreateFromFile(typeof(VcxPrjFilter), theFile);
	}

	public bool AddFilter(string Name, string theFileExt)
	{
		try
		{
			if (Name != null && Name != string.Empty)
			{
				bool flag = false;
				foreach (object item in ItemGroup)
				{
					if (item is VcxFilter && (item as VcxFilter).Include == Name)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					VcxFilter vcxFilter = new VcxFilter();
					vcxFilter.Include = Name;
					vcxFilter.UniqueIdentifier = "{" + Guid.NewGuid().ToString() + "}";
					vcxFilter.Extensions = theFileExt;
					ItemGroup.Add(vcxFilter);
				}
			}
			CurFilter = Name;
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool AddFileItem(string File, Type type)
	{
		try
		{
			bool result = false;
			if (type.BaseType == typeof(VcxFltFileItem))
			{
				bool flag = false;
				foreach (object item in ItemGroup)
				{
					if (item is VcxFltFileItem && (item as VcxFltFileItem).Include.ToLower() == File.ToLower())
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					object obj = type.Assembly.CreateInstance(type.FullName);
					(obj as VcxFltFileItem).Include = File.Trim();
					(obj as VcxFltFileItem).Filter = ((CurFilter == string.Empty) ? null : CurFilter);
					ItemGroup.Add(obj);
				}
				return true;
			}
			return result;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool AddFileItem(string theFilter, string theFile, Type type)
	{
		bool flag = AddFilter(theFilter, null);
		if (flag)
		{
			flag = AddFileItem(theFile, type);
		}
		return flag;
	}
}

// VisualEmbed.ProjectSupport.VcxProject
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VisualEmbed.ProjectSupport;

[XmlRoot(Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
[XmlType("Project")]
public class VcxProject : BaseProject
{
	public class VcxProps : List<XmlElement>
	{
		[XmlIgnore]
		private XmlElement _selected;

		[XmlIgnore]
		public XmlElement CurItem => _selected;

		[XmlIgnore]
		public string Type
		{
			get
			{
				if (_selected != null)
				{
					return _selected.Name;
				}
				return null;
			}
		}

		[XmlIgnore]
		public string Value
		{
			get
			{
				if (_selected != null)
				{
					return _selected.InnerText;
				}
				return null;
			}
			set
			{
				if (_selected != null)
				{
					_selected.InnerText = value;
				}
			}
		}

		public string GetAttribute(string theName)
		{
			if (_selected != null)
			{
				return _selected.GetAttribute(theName);
			}
			return null;
		}

		private void SetAttribute(XmlElement theElm, string theName, object theValue, bool IsAllowEmpty = false)
		{
			if (theElm == null || theName == null || !(theName != string.Empty))
			{
				return;
			}
			string text;
			if (theValue == null)
			{
				text = null;
			}
			else
			{
				text = theValue.ToString();
				if (theValue.GetType() == typeof(bool))
				{
					text = text.ToLower();
				}
			}
			if (text != null)
			{
				if (IsAllowEmpty || text != string.Empty)
				{
					theElm.SetAttribute(theName, text);
				}
				else
				{
					theElm.RemoveAttribute(theName);
				}
			}
			else
			{
				theElm.RemoveAttribute(theName);
			}
		}

		public void SetAttribute(string theName, object theValue, bool IsAllowEmpty = false)
		{
			SetAttribute(_selected, theName, theValue, IsAllowEmpty);
		}

		public void RemoveAttribute(string theName)
		{
			if (_selected != null)
			{
				_selected.RemoveAttribute(theName);
			}
		}

		public bool AddItem(string theType, object theValue, bool IsValuePrefer, params object[] theAttrs)
		{
			try
			{
				string text;
				if (theValue == null)
				{
					text = null;
				}
				else
				{
					text = theValue.ToString();
					if (theValue.GetType() == typeof(bool))
					{
						text = text.ToLower();
					}
				}
				bool flag;
				if (IsValuePrefer)
				{
					flag = text != null && text.Trim() != string.Empty;
				}
				else if (theAttrs != null && (theAttrs.Length & 1) == 0)
				{
					flag = false;
					for (int i = 0; i < theAttrs.Length; i += 2)
					{
						if (theAttrs[i] != null && theAttrs[i].ToString() != string.Empty && theAttrs[i + 1] != null && theAttrs[i + 1].ToString() != string.Empty)
						{
							flag = true;
						}
					}
				}
				else
				{
					flag = false;
				}
				if (flag)
				{
					XmlElement xmlElement = _doc.CreateElement(string.Empty, theType, _xmlns);
					xmlElement.InnerText = ((text == null) ? text : text.Trim());
					if (theAttrs != null && (theAttrs.Length & 1) == 0)
					{
						for (int j = 0; j < theAttrs.Length; j += 2)
						{
							SetAttribute(xmlElement, theAttrs[j].ToString(), theAttrs[j + 1]);
						}
					}
					Add(xmlElement);
					_selected = xmlElement;
					return flag;
				}
				return flag;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool AddItem(string theType, object theValue, params object[] theAttrs)
		{
			return AddItem(theType, theValue, IsValuePrefer: true, theAttrs);
		}

		public void RemoveItem(XmlElement Item)
		{
			if (_selected == Item)
			{
				Walkthrough(IsContinue: true);
			}
			Remove(Item);
		}

		public void RemoveItem()
		{
			if (_selected != null)
			{
				RemoveItem(_selected);
			}
		}

		public bool SelectItem(XmlElement Item)
		{
			bool num = IndexOf(Item) >= 0;
			if (num)
			{
				_selected = Item;
				return num;
			}
			_selected = null;
			return num;
		}

		public bool Walkthrough(bool IsContinue)
		{
			int num;
			if (!IsContinue)
			{
				_selected = null;
				num = 0;
			}
			else
			{
				num = IndexOf(_selected);
				num = ((num >= 0) ? (num + 1) : 0);
			}
			_selected = ((base.Count > num) ? base[num] : null);
			return _selected != null;
		}
	}

	[XmlType("PropertyGroup")]
	public class VcxPropertyGroup
	{
		[XmlAttribute]
		public string Label;

		[XmlAttribute]
		public string Condition;

		[XmlAnyElement]
		public VcxProps Props = new VcxProps();
	}

	[XmlType("ProjectConfiguration")]
	public class VcxProjectConfiguration
	{
		[XmlAttribute]
		public string Include;

		[XmlElement]
		public string Configuration;

		[XmlElement]
		public string Platform;
	}

	public class VcxFileItem
	{
		[XmlAttribute("Include")]
		public string Include;

		[XmlAnyElement]
		public VcxProps Metadatas = new VcxProps();
	}

	[XmlType("Text")]
	public class VcxText : VcxFileItem
	{
	}

	[XmlType("ClCompile")]
	public class VcxCItem : VcxFileItem
	{
	}

	[XmlType("ClInclude")]
	public class VcxInc : VcxFileItem
	{
	}

	[XmlType("AsmItem")]
	public class VcxAsmItem : VcxFileItem
	{
	}

	[XmlType("Object")]
	public class VcxObj : VcxFileItem
	{
	}

	[XmlType("Library")]
	public class VcxLib : VcxFileItem
	{
	}

	[XmlType("None")]
	public class VcxNone : VcxFileItem
	{
	}

	[XmlType("CustomItem")]
	public class VcxCustomItem : VcxFileItem
	{
	}

	[XmlIgnore]
	public static readonly string AttrCond = "Condition";

	[XmlIgnore]
	private static XmlDocument _doc = new XmlDocument();

	[XmlIgnore]
	private static readonly string _xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

	[XmlAttribute]
	public string DefaultTargets;

	[XmlAttribute]
	public string ToolsVersion;

	[XmlElement(Type = typeof(VcxPropertyGroup), ElementName = "PropertyGroup")]
	public List<VcxPropertyGroup> PropertyGroups = new List<VcxPropertyGroup>();

	[XmlArrayItem(Type = typeof(VcxText))]
	[XmlArrayItem(Type = typeof(VcxCItem))]
	[XmlArrayItem(Type = typeof(VcxInc))]
	[XmlArrayItem(Type = typeof(VcxAsmItem))]
	[XmlArrayItem(Type = typeof(VcxObj))]
	[XmlArrayItem(Type = typeof(VcxLib))]
	[XmlArrayItem(Type = typeof(VcxNone))]
	[XmlArrayItem(Type = typeof(VcxCustomItem))]
	[XmlArrayItem(Type = typeof(VcxProjectConfiguration))]
	public List<object> ItemGroup = new List<object>();

	[XmlIgnore]
	public List<VcxProjectConfiguration> PrjCfgs = new List<VcxProjectConfiguration>();

	public static VcxProject Create(Stream theStream)
	{
		return (VcxProject)BaseProject.Create(typeof(VcxProject), theStream);
	}

	public static VcxProject Create(string theXml)
	{
		return (VcxProject)BaseProject.Create(typeof(VcxProject), theXml);
	}

	public static VcxProject CreateFromFile(string theFile)
	{
		return (VcxProject)BaseProject.CreateFromFile(typeof(VcxProject), theFile);
	}

	public object AddFileItem(string File, Type type)
	{
		try
		{
			object result = null;
			if (type.BaseType == typeof(VcxFileItem))
			{
				result = type.Assembly.CreateInstance(type.FullName);
				(result as VcxFileItem).Include = File;
				ItemGroup.Add(result);
				return result;
			}
			return result;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private new bool Save(Stream theStream)
	{
		return true;
	}

	private new bool Save(out string theXml)
	{
		theXml = string.Empty;
		return true;
	}

	private new bool Save(string theFile)
	{
		return true;
	}

	public bool Save(string theFile, string theTemplate)
	{
		try
		{
			FileStream fileStream = new FileStream(theTemplate, FileMode.Open, FileAccess.Read);
			StreamReader streamReader = new StreamReader(fileStream);
			string text = streamReader.ReadToEnd();
			streamReader.Dispose();
			fileStream.Dispose();
			string text2 = "  <ItemGroup Label=\"ProjectConfigurations\">\r\n";
			foreach (VcxProjectConfiguration prjCfg in PrjCfgs)
			{
				text2 += $"    <ProjectConfiguration Include=\"{prjCfg.Include}\">\r\n      <Configuration>{prjCfg.Configuration}</Configuration>\r\n      <Platform>{prjCfg.Platform}</Platform>\r\n    </ProjectConfiguration>\r\n";
			}
			text2 += "  </ItemGroup>\r\n";
			text = text.Replace("$$$ProjectConfigurations$$$", text2);
			bool flag = base.Save(out text2);
			if (flag)
			{
				int startIndex = text2.IndexOf("<Project");
				startIndex = text2.IndexOf(">", startIndex);
				text2 = text2.Substring(startIndex + 1);
				text2 = text2.Replace("</Project>", string.Empty);
				text = text.Replace("$$$PropertyAndFiles$$$", text2);
				FileStream fileStream2 = new FileStream(theFile, FileMode.Create, FileAccess.ReadWrite);
				StreamWriter streamWriter = new StreamWriter(fileStream2);
				streamWriter.Write(text);
				streamWriter.Dispose();
				fileStream2.Dispose();
				return flag;
			}
			return flag;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
