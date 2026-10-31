using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace Skoofidons.SharedSettings.V1
{
    public static class PreferencePersistence
    {
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        public static void Save(Action save,params MelonPreferences_Category[] categories)
        {
            var categoryFile=typeof(MelonPreferences_Category).GetField("File",Fields);
            var defaultFile=typeof(MelonPreferences).GetField("DefaultFile",Fields);
            if(categoryFile==null||defaultFile==null)throw new IOException("Unsupported preference persistence API.");
            var files=categories.Select(c=>categoryFile.GetValue(c)??defaultFile.GetValue(null)).Distinct().ToArray();
            CheckAndSave(save,files);
        }
        public static void CheckAndSave(Action save,IEnumerable<object> targetFiles)
        {
            var files=targetFiles.ToArray();
            foreach(var file in files)Check(file);
            save();
            // MelonLoader 0.7.3 catches IO failures and latches WasError instead of throwing.
            foreach(var file in files)Check(file);
        }
        private static void Check(object file)
        {
            if(file==null)throw new IOException("Preference file is unavailable.");
            var type=file.GetType();
            var error=type.GetProperty("WasError",Fields)?.GetValue(file);
            var enabled=type.GetField("ShouldSave",Fields)?.GetValue(file);
            if(error is not bool failed||enabled is not bool shouldSave)throw new IOException("Unsupported preference file status API.");
            if(failed||!shouldSave)throw new IOException("Preferences were not saved. Check file permissions and restart the game before retrying.");
        }
    }
}
