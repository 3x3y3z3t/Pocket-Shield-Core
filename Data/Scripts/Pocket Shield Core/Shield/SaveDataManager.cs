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
        private const string c_SectionPlayerData = "PlayerData";
        private const string c_SectionNpcData = "NpcData";
        
        private Dictionary<long, float> m_PlayerData = new Dictionary<long, float>();
        private Dictionary<long, float> m_NpcData = new Dictionary<long, float>();

        private Logger m_Logger = null;

        public SaveDataManager(Logger _logger)
        {
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

            int errorCount = 0;
            try
            {
                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(c_SavedataFilename, typeof(PocketShieldCore.SaveDataManager));
                string content = reader.ReadToEnd();
                reader.Close();

                MyIni ini = new MyIni();
                MyIniParseResult result;
                if (!ini.TryParse(content, out result))
                {
                    m_Logger.WriteLine("  Ini parse failed: " + result.ToString(), 2);
                    return false;
                }

                errorCount += TryParseSection(ini, c_SectionPlayerData, m_PlayerData);
                errorCount += TryParseSection(ini, c_SectionNpcData, m_NpcData);
            }
            catch (Exception _e)
            {
                m_Logger.WriteLine("  >> Exception << " + _e.Message, 1);
                return false;
            }
            
            m_Logger.WriteLine("  Loaded " + m_PlayerData.Keys.Count + " Player, " + m_NpcData.Keys.Count + " NPC shield data, got " + errorCount + " error(s)", 2);
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

            MyIni ini = new MyIni();

            ini.AddSection(c_SectionPlayerData);
            ini.AddSection(c_SectionNpcData);

            foreach (KeyValuePair<long, float> pair in m_PlayerData)
            {
                ini.Set(c_SectionPlayerData, pair.Key.ToString(), pair.Value);
            }
            foreach (KeyValuePair<long, float> pair in m_NpcData)
            {
                ini.Set(c_SectionNpcData, pair.Key.ToString(), pair.Value);
            }

            string data = ini.ToString();
            try
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(c_SavedataFilename, typeof(SaveDataManager));
                writer.WriteLine(data);
                writer.Flush();
                writer.Close();
            }
            catch (Exception _e)
            {
                m_Logger.WriteLine("  >> Exception << Error during saving Shield data: " + _e.Message, 1);
                return false;
            }

            m_Logger.WriteLine("SaveData done", 1);
            return true;
        }

        public void UpdatePlayerData(long _playerUid, float _value)
        {
            m_PlayerData[_playerUid] = _value;
        }

        public void UpdateNpcData(long _characterId, float _value)
        {
            m_NpcData[_characterId] = _value;
        }

        public float GetPlayerData(long _playerUid)
        { 
            if (!m_PlayerData.ContainsKey(_playerUid))
                return 0.0f;

            return m_PlayerData[_playerUid];
        }

        public float GetNpcData(long _characterId)
        {
            if (!m_NpcData.ContainsKey(_characterId))
                return 0.0f;

            return m_NpcData[_characterId];
        }

        private int TryParseSection(MyIni _iniData, string _section, Dictionary<long, float> _data)
        {
            if (!_iniData.ContainsSection(_section))
                return 0;

            int errorCount = 0;

            List<MyIniKey> keys = new List<MyIniKey>();
            _iniData.GetKeys(_section, keys);

            foreach (MyIniKey key in keys)
            {
                long pid;
                if (!long.TryParse(key.Name, out pid))
                {
                    ++errorCount;
                    m_Logger.WriteLine("  Ignoring error key in section " + _section + ", key=" + key.Name, 2);
                    continue;
                }
                _data[pid] = (float)_iniData.Get(key).ToDouble(0.0f);
            }

            return errorCount;
        }


    }
}
