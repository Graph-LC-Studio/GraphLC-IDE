using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using GraphLC_IDE.Functions;
using GraphLC_IDE.Extensions;

namespace GraphLC_IDE.AppConfig
{
    /// <summary>
    /// 处理 Module 信息
    /// </summary>
    public class ModuleInfo
    {
        private CfgLoader obj = null;
        public CfgLoader Obj
        {
            get => obj;
        }

        private CfgLoader cache = null;
        public CfgLoader Cache
        {
            get => cache;
        }

        public string Name { get; } = "";

        public bool HighlightEnabled { get; } = false;
        public string LightHighlight { get; } = "";
        public string DarkHighlight { get; } = "";

        public bool BuildEnabled { get; } = false;
        public string BuildProgramPath { get; } = "";
        public string BuildCommand { get; } = "";

        public bool RunEnabled { get; } = false;
        public bool RunWithSimpleShell { get; } = true;
        public string RunCommand { get; } = "";
        public bool JudgeEnabled { get; } = false;
        
        public bool SupportCompletion { get; } = false;
        public string[] CompletionKey { get; } = null;
        public List<EditorCompletionData> CompletionList { get; } = null;

        public string[] Bits { get; } = null;
        public string[] Modes { get; } = null;
        public string[] Options { get; } = null;
        public string[] CodeSuffix { get; } = null;

        public bool EditorPluginEnabled { get; } = false;
        public GlcEditorPlugin.IEditorPlugin EditorPlugin { get; } = null;

        public bool ErrorAnalysisEnabled { get; } = false;
        public int ErrorAnalysisIntervals { get; } = 0;
        public int ErrorAnalysisTimeLimit { get; } = 0;
        public string ErrorAnalysisCommand { get; } = null;
        public GlcErrorAnalysisPlugin.IErrorAnalysisPlugin ErrorAnalysisPlugin { get; } = null;

        public string Version { get; } = "";

        #region Cache.cfg
        public string Bit
        {
            get => cache["bit"].ToString();
            set
            {
                cache["bit"] = value;
                cache.Save();
            }
        }
        public string Mode
        {
            get => cache["mode"].ToString();
            set
            {
                cache["mode"] = value;
                cache.Save();
            }
        }
        public string Option
        {
            get => cache["option"].ToString();
            set
            {
                cache["option"] = value;
                cache.Save();
            }
        }
        public string AttachCommand
        {
            get => cache["command"].ToString();
            set
            {
                cache["command"] = value;
                cache.Save();
            }
        }
        public string StartupParameter
        {
            get => cache["parameter"].ToString();
            set
            {
                cache["parameter"] = value;
                cache.Save();
            }
        }
        #endregion

        public string GetHandledCommand(string command, string codeFile, string attach = "")
        {
            int posPre, posSuf;
            var fileDir = codeFile.Substring(0, posPre = codeFile.LastIndexOf('\\'));
            var fileSuf = codeFile.Substring(posSuf = codeFile.LastIndexOf('.'));
            var fileName = codeFile.Substring(posPre + 1, posSuf - posPre - 1);
            var tmp = command;
            tmp = tmp.Replace("${file}", codeFile);
            tmp = tmp.Replace("${filedir}", fileDir);
            tmp = tmp.Replace("${filesuf}", fileSuf);
            tmp = tmp.Replace("${filename}", fileName);
            tmp = tmp.Replace("${command}", attach);
            tmp = tmp.Replace("${mod}", this.Name);
            tmp = tmp.Replace("${moddir}", Path.Combine(AppInfo.Path, "Config", "Module", this.Name));
            tmp = tmp.Replace("${path}", AppDomain.CurrentDomain.BaseDirectory);
            tmp = tmp.Replace("${time}", DateTime.Now.ToString("yyyyMMdd_HHmmssfffffff"));
            return tmp;
        }

        public ModuleInfo(string srcPath, string cachePath, string name)
        {
            try
            {
                Name = name;

                obj = new CfgLoader(srcPath);
                cache = new CfgLoader(cachePath);

                // 版本信息
                Version = obj["version"].ToString();
                
                // 高亮
                if (HighlightEnabled = (bool)obj["highlight"]["enabled"])
                {
                    LightHighlight = obj["highlight"]["light"].ToString();
                    DarkHighlight = obj["highlight"]["dark"].ToString();
                }

                // 编译信息
                BuildEnabled = (bool)obj["build"]["enabled"];
                BuildProgramPath = obj["build"]["program-path"].ToString();
                BuildCommand = obj["build"]["command"].ToString();

                RunEnabled = (bool)obj["run"]["enabled"];
                RunWithSimpleShell = (bool)obj["run"]["use-simple-shell"];
                RunCommand = obj["run"]["command"].ToString();
                JudgeEnabled = (bool)obj["run"]["judge"]["enabled"];

                Bits = obj["bits"].ToString().ESplit();
                Modes = obj["modes"].ToString().ESplit();
                Options = obj["options"].ToString().ESplit();
                CodeSuffix = obj["code-suffix"].ToString().ESplit();

                if (obj.Obj.ContainsKey("completion"))
                {
                    if (SupportCompletion = (bool)obj["completion"]["enabled"])
                    {
                        CompletionKey = obj["completion"]["key"].ToString().ESplit();
                        CompletionList = new List<EditorCompletionData>();
                        foreach (var iter in obj["completion"]["list"])
                        {
                            var array = iter as JArray;
                            CompletionList.Add(new EditorCompletionData(array[0].ToString(), array[1].ToString(), array[2].ToString(),
                                array[3].ToString() == "" ? null : Helper.GetBitmapImage(Path.Combine(AppInfo.Path, "Config", "Module", name, array[3].ToString()))));
                        }
                    }
                }

                // Editor 插件
                if (obj.Obj.ContainsKey("editor-plugin"))
                {
                    if (EditorPluginEnabled = (bool)obj["editor-plugin"]["enabled"])
                    {
                        EditorPlugin = PluginLoadContext<GlcEditorPlugin.IEditorPlugin>.CreateCommands(new string[1] { Path.Combine(AppInfo.Path, "Config", "Module", Name, obj["editor-plugin"]["path"].ToString()) })[0];
                        try
                        {
                            EditorPlugin.PluginLoaded();
                        }
                        catch(Exception ex)
                        {
                            EditorPluginEnabled = false;
                            Log.WriteErr(ex.Message, "ModuleInformation.cs - 编辑器拓展");
                            throw new Exception("加载编辑器拓展时出错, 插件启动失败\n" + ex.Message);
                        }
                    }
                }

                // 错误分析
                if (obj.Obj.ContainsKey("error-analysis"))
                {
                    ErrorAnalysisEnabled = (bool)obj["error-analysis"]["enabled"];
                    if (ErrorAnalysisEnabled)
                    {
                        try
                        {
                            ErrorAnalysisIntervals = (int)obj["error-analysis"]["intervals"];
                            ErrorAnalysisTimeLimit = (int)obj["error-analysis"]["timeLimit"];
                            ErrorAnalysisCommand = obj["error-analysis"]["command"].ToString();
                            ErrorAnalysisPlugin = PluginLoadContext<GlcErrorAnalysisPlugin.IErrorAnalysisPlugin>.CreateCommands(new string[1] { Path.Combine(AppInfo.Path, "Config", "Module", Name, obj["error-analysis"]["path"].ToString()) })[0];
                            ErrorAnalysisPlugin.PluginLoaded();
                        }
                        catch (Exception ex)
                        {
                            ErrorAnalysisEnabled = false;
                            Log.WriteErr(ex.Message, "ModuleInformation.cs - 错误分析");
                            throw new Exception("加载错误分析时出错, 插件启动失败" + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "ModuleInformation.cs");
                obj = null;   // 通过ModuleInformation.Obj == null 检测是否加载成功
            }
        }
    }
}
