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
