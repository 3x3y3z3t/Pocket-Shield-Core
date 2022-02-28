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
        public float ManualEnergy;
        public float AutoEnergy;
        public bool AutoTurnedOn;
    }

    public class SaveDataManager
    {
        private const string c_SavedataFilename = "PocketShield_savedata.dat";
        private const string c_SectionCommon = "Common";

        private Dictionary<long, SaveData> m_EnergyData = null;

        private Dictionary<long, CharacterShieldManager> m_EmittersRef = null;
        private Logger m_Logger = null;
        
        
        public SaveDataManager(Dictionary<long, CharacterShieldManager> _emitters, Logger _logger)
        {
            m_EnergyData = new Dictionary<long, SaveData>();

            m_EmittersRef = _emitters;
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

        public void ForceRemoveEntry(long _entityId)
        {
            if (m_EnergyData.ContainsKey(_entityId))
                m_EnergyData.Remove(_entityId);
        }

        public void RemoveShieldEnergy(long _entityId, bool _isManual)
        {
            if (!m_EnergyData.ContainsKey(_entityId))
                return;

            if (_isManual)
                m_EnergyData[_entityId].ManualEnergy = 0.0f;
            else
            {
                m_EnergyData[_entityId].AutoEnergy = 0.0f;
                m_EnergyData[_entityId].AutoTurnedOn = true;
            }
        }

        public float GetSavedManualShieldEnergy(long _entityId)
        {
            if (m_EnergyData.ContainsKey(_entityId))
                return m_EnergyData[_entityId].ManualEnergy;

            return 0.0f;
        }

        public float GetSavedAutoShieldEnergy(long _entityId)
        {
            if (m_EnergyData.ContainsKey(_entityId))
                return m_EnergyData[_entityId].AutoEnergy;

            return 0.0f;
        }

        public bool GetSavedAutoShieldTurnedOn(long _entityId)
        {
            if (m_EnergyData.ContainsKey(_entityId))
                return m_EnergyData[_entityId].AutoTurnedOn;

            return true;
        }

        public void SetSaveAutoShieldEnergy(long _entityId, float _value)
        {
            if (m_EnergyData.ContainsKey(_entityId))
                m_EnergyData[_entityId].AutoEnergy = _value;
        } 

        public void SetSaveAutoShieldTurnedOn(long _entityId, bool _turnedOn)
        {
            if (m_EnergyData.ContainsKey(_entityId))
                m_EnergyData[_entityId].AutoTurnedOn = _turnedOn;
        } 

        public void ApplySavedataOnceBeforeSim()
        {
            foreach (var key in m_EnergyData.Keys)
            {
                if (m_EmittersRef.ContainsKey(key))
                {
                    if (m_EmittersRef[key].ManualEmitter != null)
                        m_EmittersRef[key].ManualEmitter.Energy = m_EnergyData[key].ManualEnergy;

                    if (m_EmittersRef[key].AutoEmitter != null)
                    {
                        m_EmittersRef[key].AutoEmitter.Energy = m_EnergyData[key].AutoEnergy;
                        m_EmittersRef[key].AutoEmitter.IsTurnedOn = m_EnergyData[key].AutoTurnedOn;
                    }
                }
            }
        }

        public void Update()
        {
            foreach (var pair in m_EmittersRef)
            {
                if (!m_EnergyData.ContainsKey(pair.Key))
                    continue;

                if (pair.Value.ManualEmitter != null)
                    m_EnergyData[pair.Key].ManualEnergy = pair.Value.ManualEmitter.Energy;
                else
                    m_EnergyData[pair.Key].ManualEnergy = 0.0f;

                if (pair.Value.AutoEmitter != null)
                {
                    m_EnergyData[pair.Key].AutoEnergy = pair.Value.AutoEmitter.Energy;
                    m_EnergyData[pair.Key].AutoTurnedOn = pair.Value.AutoEmitter.IsTurnedOn;
                }
                else
                {
                    m_EnergyData[pair.Key].AutoEnergy = 0.0f;
                    m_EnergyData[pair.Key].AutoTurnedOn = true;
                }
            }
        }

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

                m_EnergyData[pid] = new SaveData()
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
