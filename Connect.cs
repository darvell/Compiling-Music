using System;
using System.IO;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Un4seen.Bass;
using Microsoft.VisualStudio.CommandBars;
using System.Resources;
using System.Reflection;
using System.Globalization;

namespace CompilingMusic
{
    public class Connect : IDTExtensibility2
    {
        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private OutputWindowPane outputWindowPane;
        private EnvDTE.BuildEvents buildEvents;
        public Random rnd = new Random();
        public int songHandle = 0;
        public string userDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public string songFile;
        IniParser parser = null;
        Settings settings = new Settings();
        Set currentSet = new Set();
        DateTime settingLastModified;
        
        /*******************IGNORE******************/
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnStartupComplete(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
        }
        /*********************************************/
        
        public string getRandomFile(string filepath)
        {
            string[] files = Directory.GetFiles(filepath);
            return files[rnd.Next(0, files.Length)];
        }

        /// <summary>
        /// Loads a random set file from the folder defined in settings and parses it into the global variable "currentSet".
        /// </summary>
        public void setRandomSet()
        {
            currentSet = new Set();
            string[] files = Directory.GetFiles(settings.setDirectory,"*.ini");
            string selectedSet = files[rnd.Next(0, files.Length)];
            parser = new IniParser(selectedSet);
            currentSet.basePath = parser.GetSetting("Set", "BasePath");
            currentSet.compileSong = parser.GetSetting("Set", "CompileSong").Split(';');
            currentSet.successSong = parser.GetSetting("Set", "SuccessSong").Split(';');
            currentSet.failSong = parser.GetSetting("Set", "FailSong").Split(';');
        }

        /// <summary>
        /// Parses CompilingMusicSettings.ini into the global variable "settings".
        /// </summary>
        private void loadSettings()
        {
            FileInfo settingInfo = new FileInfo(userDir + "\\Visual Studio 2010\\Addins\\CompilingMusicSettings.ini");
            if (settingLastModified == settingInfo.LastWriteTime)
                return;
            parser = new IniParser(userDir + "\\Visual Studio 2010\\Addins\\CompilingMusicSettings.ini");
            settings.bassUser = parser.GetSetting("bass", "email");
            settings.bassCode = parser.GetSetting("bass", "code");
            settings.setMode = Boolean.Parse(parser.GetSetting("options", "setMode"));
            settings.setDirectory = parser.GetSetting("options", "setDirectory");
            settings.randomCompileDirectory = parser.GetSetting("options", "randomCompileDirectory");
            settings.randomFailDirectory = parser.GetSetting("options", "randomFailDirectory");
            settings.randomSuccessDirectory = parser.GetSetting("options", "randomSuccessDirectory");
            settings.STFU = Boolean.Parse(parser.GetSetting("options", "STFU"));
            settingLastModified = settingInfo.LastWriteTime;
        }

        /// <summary>
        /// Frees the audio stream and BASS module to prevent memory leaks.
        /// </summary>
        private void stopAndFree()
        {
            Bass.BASS_StreamFree(songHandle);
            Bass.BASS_Free();
            songHandle = 0;
        }

        private bool isValidSong(string path)
        {
            if (path == null)
                return false;

            switch (path.ToLower().Substring(path.Length - 3))
            {
                case "wav":
                case "aiff":
                case "mp3":
                case "mp2":
                case "mp1":
                case "ogg":
                    return true;
                    break;
                default:
                    return false;
                    break;
            }
        }

        public Connect()
        {
        }

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            // Register events.
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;
            OutputWindow outputWindow = (OutputWindow)_applicationObject.Windows.Item(Constants.vsWindowKindOutput).Object;
            outputWindowPane = outputWindow.OutputWindowPanes.Item("Build");
            EnvDTE.Events events = _applicationObject.Events;
            buildEvents = (EnvDTE.BuildEvents)events.BuildEvents;
            buildEvents.OnBuildBegin += new _dispBuildEvents_OnBuildBeginEventHandler(this.OnBuildBegin);
            buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);

            // Read Config
            loadSettings();
            BassNet.Registration(settings.bassUser, settings.bassCode);
        }


        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {

            if (buildEvents != null)
            {
                buildEvents.OnBuildBegin -= new _dispBuildEvents_OnBuildBeginEventHandler(this.OnBuildBegin);
                buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
            }
        }

        public void OnBuildBegin(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {
            if (songHandle != 0)
                stopAndFree();

            loadSettings();

            if (settings.STFU)
                return;

            if ((Action != EnvDTE.vsBuildAction.vsBuildActionRebuildAll) && (Action != EnvDTE.vsBuildAction.vsBuildActionBuild))
                return;

            if (settings.setMode)
                setRandomSet();

            if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                if (!settings.setMode)
                    songFile = getRandomFile(settings.randomCompileDirectory);
                else
                    songFile = currentSet.basePath + currentSet.compileSong[rnd.Next(0,currentSet.compileSong.Length)];

                if (!isValidSong(songFile))
                {
                    Bass.BASS_Free();
                    return;
                }

                songHandle = Bass.BASS_StreamCreateFile(songFile, 0L, 0L, BASSFlag.BASS_DEFAULT);
                
                if (songHandle != 0)
                    Bass.BASS_ChannelPlay(songHandle, false);
            }
        }

        public void OnBuildDone(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {

            if (songHandle != 0)
                stopAndFree();
            
            loadSettings();

            if (settings.STFU)
                return;

            if ((Action != EnvDTE.vsBuildAction.vsBuildActionRebuildAll) && (Action != EnvDTE.vsBuildAction.vsBuildActionBuild))
                return;

            if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                if (_applicationObject.Solution.SolutionBuild.LastBuildInfo != 0)
                {
                    if (!settings.setMode)
                        songFile = getRandomFile(settings.randomFailDirectory);
                    else
                        songFile = currentSet.basePath + currentSet.failSong[rnd.Next(0, currentSet.failSong.Length)];

                    if (!isValidSong(songFile))
                        return;

                    songHandle = Bass.BASS_StreamCreateFile(songFile, 0L, 0L, BASSFlag.BASS_DEFAULT);
                }
                else
                {
                    if (!settings.setMode)
                        songFile = getRandomFile(settings.randomSuccessDirectory);
                    else
                        songFile = currentSet.basePath + currentSet.successSong[rnd.Next(0, currentSet.successSong.Length)];

                    if (!isValidSong(songFile))
                        return;

                    songHandle = Bass.BASS_StreamCreateFile(songFile, 0L, 0L, BASSFlag.BASS_DEFAULT);
                }
                if (songHandle != 0)
                    Bass.BASS_ChannelPlay(songHandle, false);
            }
        }
    }
}

