// ;
using ExShared;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace PocketShieldCore
{
    class SaveData
    {
        public float ManualEnergy = 0.0f;
        public float AutoEnergy = 0.0f;
        public bool AutoTurnedOn = true;
    }

    public class SaveDataManager
    {
        private const string c_SavedataFilename = "PocketShield_savedata.dat";
        private const string c_SectionCommon = "Common";

        private Dictionary<long, SaveData> m_SaveData = null;

        private Dictionary<long, CharacterShieldInfo> m_CharacterInfosRef = null;
        private Logger m_Logger = null;
        
        
        public SaveDataManager(Dictionary<long, CharacterShieldInfo> _charInfos, Logger _logger)
        {
            m_SaveData = new Dictionary<long, SaveData>();

            m_CharacterInfosRef = _charInfos;
            m_Logger = _logger;
            LoadData();
        }

        private bool LoadData()
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

            m_Logger.WriteLine("  Loaded " + m_SaveData.Count + " shield data, got " + errorCount + " error(s)", 2);
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
            foreach (var pair in m_SaveData)
            {
                if (pair.Value.ManualEnergy > 0.0f || pair.Value.AutoEnergy > 0.0f)
                {
                    iniData.Set(c_SectionCommon, pair.Key.ToString(), pair.Value.ManualEnergy + "," + pair.Value.AutoEnergy + "," + pair.Value.AutoTurnedOn);
                }
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

        public void Update()
        {
            foreach (var pair in m_CharacterInfosRef)
            {
                if (!pair.Value.HasAnyEmitter)
                {
                    if (m_SaveData.ContainsKey(pair.Key))
                    {
                        m_SaveData[pair.Key].ManualEnergy = 0.0f;
                        m_SaveData[pair.Key].AutoEnergy = 0.0f;
                        m_SaveData[pair.Key].AutoTurnedOn = true;
                    }

                    continue;
                }

                if (!m_SaveData.ContainsKey(pair.Key))
                    m_SaveData[pair.Key] = new SaveData();

                if (pair.Value.ManualEmitter != null)
                    m_SaveData[pair.Key].ManualEnergy = pair.Value.ManualEmitter.Energy;
                else
                    m_SaveData[pair.Key].ManualEnergy = 0.0f;
                
                if (pair.Value.AutoEmitter != null)
                {
                    m_SaveData[pair.Key].AutoEnergy = pair.Value.AutoEmitter.Energy;
                    m_SaveData[pair.Key].AutoTurnedOn = pair.Value.AutoEmitter.IsTurnedOn;
                }
                else
                {
                    m_SaveData[pair.Key].AutoEnergy = 0.0f;
                    m_SaveData[pair.Key].AutoTurnedOn = true;
                }
            }
        }

        public void ApplySavedataOnceBeforeSim()
        {
            foreach (var key in m_SaveData.Keys)
            {
                if (!m_CharacterInfosRef.ContainsKey(key))
                    continue;

                if (m_CharacterInfosRef[key].ManualEmitter != null)
                    m_CharacterInfosRef[key].ManualEmitter.Energy = m_SaveData[key].ManualEnergy;

                if (m_CharacterInfosRef[key].AutoEmitter != null)
                {
                    m_CharacterInfosRef[key].AutoEmitter.Energy = m_SaveData[key].AutoEnergy;
                    m_CharacterInfosRef[key].AutoEmitter.IsTurnedOn = m_SaveData[key].AutoTurnedOn;
                }
            }
        }

        //public void ForceRemoveEntry(long _entityId)
        //{
        //    if (m_SaveData.ContainsKey(_entityId))
        //        m_SaveData.Remove(_entityId);
        //}

        //public void RemoveShieldEnergy(long _entityId, bool _isManual)
        //{
        //    if (!m_SaveData.ContainsKey(_entityId))
        //        return;

        //    if (_isManual)
        //        m_SaveData[_entityId].ManualEnergy = 0.0f;
        //    else
        //    {
        //        m_SaveData[_entityId].AutoEnergy = 0.0f;
        //        m_SaveData[_entityId].AutoTurnedOn = true;
        //    }
        //}

        public float GetSavedManualShieldEnergy(long _entityId)
        {
            if (m_SaveData.ContainsKey(_entityId))
                return m_SaveData[_entityId].ManualEnergy;

            return 0.0f;
        }

        public float GetSavedAutoShieldEnergy(long _entityId)
        {
            if (m_SaveData.ContainsKey(_entityId))
                return m_SaveData[_entityId].AutoEnergy;

            return 0.0f;
        }

        public bool GetSavedAutoShieldTurnedOn(long _entityId)
        {
            if (m_SaveData.ContainsKey(_entityId))
                return m_SaveData[_entityId].AutoTurnedOn;

            return true;
        }

        //public void SetSaveAutoShieldEnergy(long _entityId, float _value)
        //{
        //    if (m_SaveData.ContainsKey(_entityId))
        //        m_SaveData[_entityId].AutoEnergy = _value;
        //}

        //public void SetSaveAutoShieldTurnedOn(long _entityId, bool _turnedOn)
        //{
        //    if (m_SaveData.ContainsKey(_entityId))
        //        m_SaveData[_entityId].AutoTurnedOn = _turnedOn;
        //}

        private int TryParseData(MyIni _iniData)
        {
            int errCount = 0;
            List<MyIniKey> keys = new List<MyIniKey>();
            _iniData.GetKeys(keys);
            foreach (MyIniKey key in keys)
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

                string[] energys = _iniData.Get(key).ToString(string.Empty).Split(',');
                if (energys.Length != 3)
                {
                    ++errCount;
                    m_Logger.WriteLine("  Ignoring key [" + key.Name + "] with error value", 2);
                    continue;
                }

                float manualEnergy;
                float autoEnergy;
                bool autoTurnedOn;
                float.TryParse(energys[0], out manualEnergy);
                float.TryParse(energys[1], out autoEnergy);
                bool.TryParse(energys[2], out autoTurnedOn);

                if (manualEnergy < 0.0f)
                    manualEnergy = 0.0f;
                if (autoEnergy < 0.0f)
                    autoEnergy = 0.0f;
                if (manualEnergy == 0.0f && autoEnergy == 0.0f)
                {
                    ++errCount;
                    continue;
                }

                m_SaveData[pid] = new SaveData()
                {
                    ManualEnergy = manualEnergy,
                    AutoEnergy = autoEnergy,
                    AutoTurnedOn = autoTurnedOn
                };
            }

            return errCount;
        }

    }
}
