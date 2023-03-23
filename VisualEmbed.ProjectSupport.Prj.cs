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
