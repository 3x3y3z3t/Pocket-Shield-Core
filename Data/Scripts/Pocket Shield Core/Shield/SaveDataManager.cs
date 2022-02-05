// ;
using ExShared;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using VRage;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
using VRageMath;

namespace PocketShieldCore
{
    public class SaveDataManager
    {
        private const string c_SavedataFilename = "PocketShield_savedata.dat";
        private const string c_SectionCommon = "Common";
        
        private Dictionary<long, float> m_EnergyData = new Dictionary<long, float>();
        
        private Dictionary<long, ShieldEmitter> m_EmittersRef = new Dictionary<long, ShieldEmitter>();
        private Logger m_Logger = null;

        private List<long> m_CachedKeys = new List<long>();

        public SaveDataManager(Dictionary<long, ShieldEmitter> _emitters, Logger _logger)
        {
            m_EmittersRef = _emitters;
            m_Logger = _logger;
            LoadData();
        }

        public bool LoadData()
        {
            m_Logger.WriteLine("Loading SaveData (shield)..", 1);

            if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(c_SavedataFilename, typeof(SaveDataManager)))
            {
                m_Logger.WriteLine("  Couldn't find savedata file (" + c_SavedataFilename + ") in World Storage", 1);
                return false;
            }

            string content = string.Empty;
            try
            {
                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(c_SavedataFilename, typeof(SaveDataManager));
                content = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception _e)
            {
                m_Logger.WriteLine("  >> Exception << Error reading savedata file: " + _e.Message, 1);
                return false;
            }

            int errorCount = 0;
            MyIni iniData = new MyIni();
            MyIniParseResult result;
            if (!iniData.TryParse(content, out result))
            {
                m_Logger.WriteLine("  Ini parse failed: " + result.ToString(), 2);
                return false;
            }

            errorCount = TryParseData(iniData);

            m_Logger.WriteLine("  Loaded " + m_EnergyData.Count + " shield data, got " + errorCount + " error(s)", 2);
            m_Logger.WriteLine("Loading SaveData done", 1);
            return true;
        }

        public bool UnloadData()
        {
            m_Logger.WriteLine("Unloading SaveData (shield)..", 1);
            
            m_Logger.WriteLine("Unloading SaveData done", 1);
            return true;
        }

        public bool SaveData()
        {
            m_Logger.WriteLine("Saving SaveData (shield)...", 1);

            MyIni iniData = new MyIni();
            foreach (var pair in m_EnergyData)
            {
                if (pair.Value >= 0.0f)
                    iniData.Set(c_SectionCommon, pair.Key.ToString(), pair.Value); // only save "real value";
            }
            
            string data = iniData.ToString();
            try
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(c_SavedataFilename, typeof(SaveDataManager));
                writer.WriteLine(data);
                writer.Flush();
                writer.Close();
            }
            catch (Exception _e)
            {
                m_Logger.WriteLine("  >> Exception << Error during saving savedata: " + _e.Message, 1);
                return false;
            }

            m_Logger.WriteLine("SaveData done", 1);
            return true;
        }

        private int TryParseData(MyIni _iniData)
        {
            int errCount = 0;
            List<MyIniKey> keys = new List<MyIniKey>();
            _iniData.GetKeys(keys);
            foreach(MyIniKey key in keys)
            {
                if (key.IsEmpty)
                {
                    ++errCount;
                    m_Logger.WriteLine("  Ignoring empty key", 2);
                    continue;
                }

                long pid;
                if (!long.TryParse(key.Name, out pid))
                {
                    ++errCount;
                    m_Logger.WriteLine("  Ignoring error key [" + key.Name + "]", 2);
                    continue;
                }

                double energy = _iniData.Get(key).ToDouble(0.0);
                if (energy <= 0.0f)
                {
                    m_Logger.WriteLine("  Ignoring key [" + key.Name + "] with error value", 2);
                    continue;
                }

                m_EnergyData[pid] = (float)energy;
            }

            return errCount;
        }

        public void ApplySavedata()
        {
            foreach(var pair in m_EnergyData)
            {
                if (m_EmittersRef.ContainsKey(pair.Key))
                    m_EmittersRef[pair.Key].Energy = pair.Value;
            }
        }

        public void Update()
        {
            foreach (var pair in m_EmittersRef)
            {
                m_EnergyData[pair.Key] = pair.Value.Energy;
            }
        }
        
    }
}
