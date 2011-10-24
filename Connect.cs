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
        IniParser parser = null;
        Settings settings = new Settings();
        Set currentSet = new Set();

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
            parser = new IniParser(files[rnd.Next(0, files.Length)]);
            currentSet.basePath = parser.GetSetting("Set", "BasePath");
            currentSet.compileSong = parser.GetSetting("Set", "CompileSong");
            currentSet.successSong = parser.GetSetting("Set", "SuccessSong");
            currentSet.failSong = parser.GetSetting("Set", "FailSong");
        }

        /// <summary>
        /// Parses CompilingMusicSettings.ini into the global variable "settings".
        /// </summary>
        private void loadSettings()
        {
            parser = new IniParser("CompilingMusicSettings.ini");
            settings.bassUser = parser.GetSetting("bass", "email");
            settings.bassCode = parser.GetSetting("bass", "code");
            settings.setMode = Boolean.Parse(parser.GetSetting("options", "setMode"));
            settings.setDirectory = parser.GetSetting("options", "setDirectory");
            settings.randomCompileDirectory = parser.GetSetting("options", "randomCompileDirectory");
            settings.randomFailDirectory = parser.GetSetting("options", "randomFailDirectory");
            settings.randomSuccessDirectory = parser.GetSetting("options", "randomSuccessDirectory");
            settings.STFU = Boolean.Parse(parser.GetSetting("options", "STFU"));
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

            if (settings.STFU)
                return;

            if ((Action != EnvDTE.vsBuildAction.vsBuildActionRebuildAll) && (Action != EnvDTE.vsBuildAction.vsBuildActionBuild))
                return;

            if (settings.setMode)
                setRandomSet();

            if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                if (!settings.setMode)
                    songHandle = Bass.BASS_StreamCreateFile(getRandomFile(settings.randomCompileDirectory), 0L, 0L, BASSFlag.BASS_MUSIC_LOOP);
                else
                    songHandle = Bass.BASS_StreamCreateFile(currentSet.basePath + currentSet.compileSong, 0L, 0L, BASSFlag.BASS_MUSIC_LOOP);
              
                if (songHandle != 0)
                    Bass.BASS_ChannelPlay(songHandle, false);
            }
        }


        public void OnBuildDone(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {

            if (songHandle != 0)
                stopAndFree();

            if (settings.STFU)
                return;

            if ((Action != EnvDTE.vsBuildAction.vsBuildActionRebuildAll) && (Action != EnvDTE.vsBuildAction.vsBuildActionBuild))
                return;

            if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                if (_applicationObject.Solution.SolutionBuild.LastBuildInfo != 0)
                {
                    if (!settings.setMode)
                        songHandle = Bass.BASS_StreamCreateFile(getRandomFile(settings.randomFailDirectory), 0L, 0L, BASSFlag.BASS_MUSIC_LOOP);
                    else
                        songHandle = Bass.BASS_StreamCreateFile(currentSet.basePath + currentSet.failSong, 0L, 0L, BASSFlag.BASS_MUSIC_LOOP);
                }
                else
                {
                    if (!settings.setMode)
                        songHandle = Bass.BASS_StreamCreateFile(getRandomFile(settings.randomSuccessDirectory), 0L, 0L, BASSFlag.BASS_MUSIC_LOOP);
                    else
                        songHandle = Bass.BASS_StreamCreateFile(currentSet.basePath + currentSet.successSong, 0L, 0L, BASSFlag.BASS_MUSIC_LOOP);
                }
                if (songHandle != 0)
                    Bass.BASS_ChannelPlay(songHandle, false);
            }
        }
    }
}

