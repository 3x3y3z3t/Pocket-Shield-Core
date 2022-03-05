// ;
using ExShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace PocketShieldCore
{
    //public class Modifiers
    //{
    //    public float this[MyStringHash _key]
    //    {
    //        get { if (m_Modifiers.ContainsKey(_key)) return m_Modifiers[_key]; return 0.0f; }
    //        set { m_Modifiers[_key] = value; }
    //    }

    //    private Dictionary<MyStringHash, float> m_Modifiers = new Dictionary<MyStringHash, float>(MyStringHash.Comparer);

    //    public bool ContainsModifier(string _key) { return m_Modifiers.ContainsKey(MyStringHash.GetOrCompute(_key)); }
    //    public bool ContainsModifier(MyStringHash _key) { return m_Modifiers.ContainsKey(_key); }
    //}

    public class CharacterShieldInfo
    {
        public bool HasAnyEmitter { get { return ManualEmitter != null || AutoEmitter != null; } }
        public bool RequireSync
        {
            get
            {
                if (ManualEmitter != null && ManualEmitter.RequireSync)
                    return true;
                if (AutoEmitter != null && AutoEmitter.RequireSync)
                    return true;
                return false;
            }
        }

        public ShieldEmitter ManualEmitter = null;
        public ShieldEmitter AutoEmitter = null;

        public Logger ManualEmitterLogger = null;
        public Logger AutoEmitterLogger = null;

        public int ManualEmitterIndex = -1;
        public int AutoEmitterIndex = -1;

        public float LastAutoEnergy = 0.0f;
        public bool LastAutoTurnedOn = true;

        public CharacterShieldInfo()
        {



        }
        
        public void Update(int _skipTicks)
        {
            if (ManualEmitter != null)
            {
                ManualEmitter.Update(_skipTicks);
            }

            if (AutoEmitter != null)
            {
                AutoEmitter.Update(_skipTicks);
                LastAutoEnergy = AutoEmitter.Energy;
                LastAutoTurnedOn = AutoEmitter.IsTurnedOn;
            }

            

        }


    }

    class ShieldManager
    {
        public static Dictionary<MyStringHash, Dictionary<MyStringHash, float>> PluginModifiers = null;

        public Dictionary<long, CharacterShieldInfo> CharacterInfos { get { return m_CharacterInfos; } }
        public Dictionary<MyStringHash, PocketShieldAPIV2.ShieldEmitterProperties> EmitterProperties { get { return m_EmitterProperties; } }
            
        private Dictionary<MyStringHash, PocketShieldAPIV2.ShieldEmitterProperties> m_EmitterProperties = null;

        private Dictionary<long, CharacterShieldInfo> m_CharacterInfos = null;


        private readonly Logger m_Logger = null;

        
        public ShieldManager(Logger _logger)
        {
            m_Logger = _logger;

            m_EmitterProperties = new Dictionary<MyStringHash, PocketShieldAPIV2.ShieldEmitterProperties>(MyStringHash.Comparer);

            m_CharacterInfos = new Dictionary<long, CharacterShieldInfo>();



        }

        public void Close()
        {
            PluginModifiers = null;

            foreach (var charInfo in m_CharacterInfos.Values)
            {
                charInfo.ManualEmitter = null;
                charInfo.AutoEmitter = null;
                
                if (charInfo.ManualEmitterLogger != null)
                    charInfo.ManualEmitterLogger.Close();
                if (charInfo.AutoEmitterLogger != null)
                    charInfo.AutoEmitterLogger.Close();
            }

            
        }
        
        public ShieldEmitter AddNewEmitter(IMyCharacter _character, MyStringHash _subtypeId, bool _isManualEmitter)
        {
            m_Logger.WriteLine("> Adding new Emitter " + _subtypeId.String + "..", 4);

            if (!m_EmitterProperties.ContainsKey(_subtypeId))
            {
                m_Logger.WriteLine("  Emitter " + _subtypeId.String + " is not registered", 2);
                return null;
            }

            PocketShieldAPIV2.ShieldEmitterProperties prop = m_EmitterProperties[_subtypeId];
            if (prop.IsManual != _isManualEmitter)
            {
                m_Logger.WriteLine("  Emitter " + _subtypeId.String + "'s IsManual data mismatched (this should not happen)", 2);
                return null;
            }

            CharacterShieldInfo charInfo = m_CharacterInfos[_character.EntityId];

            string loggerName = "shield_" + _character.EntityId + "_" + Utils.GetCharacterName(_character);
            if (_isManualEmitter)
            {
                charInfo.ManualEmitterLogger = new Logger(loggerName + "_Manual");
                charInfo.ManualEmitter = new ShieldEmitter(prop, _character, charInfo.ManualEmitterLogger);

                m_Logger.WriteLine("> Manual Emitter " + _subtypeId.String + " added", 4);
                return charInfo.ManualEmitter;
            }
            else
            {
                charInfo.AutoEmitterLogger = new Logger(loggerName + "_Auto");
                charInfo.AutoEmitter = new ShieldEmitter(prop, _character, charInfo.AutoEmitterLogger);

                m_Logger.WriteLine("> Auto Emitter " + _subtypeId.String + " added", 4);
                return charInfo.AutoEmitter;
            }
        }

        public ShieldEmitter ReplaceEmitter(IMyCharacter _character, MyStringHash _subtypeId, bool _isManualEmitter)
        {
            m_Logger.WriteLine("> Replacing with Emitter " + _subtypeId.String + "..", 4);

            CharacterShieldInfo charInfo = m_CharacterInfos[_character.EntityId];
            if (_isManualEmitter)
            {
                if (charInfo.ManualEmitter == null)
                {
                    m_Logger.WriteLine("  Manual Emitter is null (this should not happen)", 2);
                    return null;
                }
            }
            else
            {
                if (charInfo.AutoEmitter == null)
                {
                    m_Logger.WriteLine("  Auto Emitter is null (this should not happen)", 2);
                    return null;
                }
            }
            
            if (!m_EmitterProperties.ContainsKey(_subtypeId))
            {
                m_Logger.WriteLine("Emitter " + _subtypeId.String + " is not registered", 2);
                return null;
            }

            PocketShieldAPIV2.ShieldEmitterProperties prop = m_EmitterProperties[_subtypeId];
            if (prop.IsManual != _isManualEmitter)
            {
                m_Logger.WriteLine("Emitter " + _subtypeId.String + "'s IsManual data mismatched (this should not happen)", 2);
                return null;
            }
            
            if (_isManualEmitter)
            {
                string old = charInfo.ManualEmitter.SubtypeId.String;
                charInfo.ManualEmitter = new ShieldEmitter(prop, _character, charInfo.ManualEmitterLogger);
                charInfo.ManualEmitterLogger.WriteLine("=====> New Manual Emitter <=====\n");
                charInfo.ManualEmitterLogger.WriteLine("  " + _subtypeId.String + "\n");
                charInfo.ManualEmitterLogger.WriteLine("================================");
                
                m_Logger.WriteLine("> Manual Emitter replaced: " + old + " -> " + _subtypeId.String, 4);
                return charInfo.ManualEmitter;
            }
            else
            {
                string old = charInfo.AutoEmitter.SubtypeId.String;
                charInfo.AutoEmitter = new ShieldEmitter(prop, _character, charInfo.AutoEmitterLogger);
                charInfo.AutoEmitterLogger.WriteLine("=====> New Auto Emitter <=====\n");
                charInfo.AutoEmitterLogger.WriteLine("  " + _subtypeId.String + "\n");
                charInfo.AutoEmitterLogger.WriteLine("================================");

                m_Logger.WriteLine("> Auto Emitter replaced: " + old + " -> " + _subtypeId.String, 4);
                return charInfo.AutoEmitter;
            }
        }

        public bool DropEmitter(IMyCharacter _character, bool _isManualEmitter)
        {
            m_Logger.WriteLine("> Character " + _character.EntityId + " is dropping some Emitter..", 4);

            CharacterShieldInfo charInfo = m_CharacterInfos[_character.EntityId];
            if (_isManualEmitter)
            {
                if (charInfo.ManualEmitter != null)
                {
                    charInfo.ManualEmitter = null;
                    charInfo.ManualEmitterLogger.Close();
                    charInfo.ManualEmitterLogger = null;

                    m_Logger.WriteLine("  Manual Emitter dropped", 2);
                    return true;
                }
            }
            else
            {
                if (charInfo.AutoEmitter != null)
                {
                    charInfo.AutoEmitter = null;
                    charInfo.AutoEmitterLogger.Close();
                    charInfo.AutoEmitterLogger = null;

                    m_Logger.WriteLine("  Auto Emitter dropped", 2);
                    return true;
                }
            }

            return false;
        }

        public void ActivateManualShield(long _characterEntityId, bool _activate)
        {
            m_Logger.Write("> Character [" + _characterEntityId + "] is trying to " + (_activate ? "activate" : "deactivate") + " Manual Emitter.. ", 2);

            CharacterShieldInfo charInfo = m_CharacterInfos[_characterEntityId];
            if (charInfo.ManualEmitter != null)
            {
                charInfo.ManualEmitter.IsActive = _activate;
                m_Logger.WriteInline("success\n", 2);
            }
            else
            {
                m_Logger.WriteInline("failed (character doesn't have Manual Emitter)\n", 2);
            }
        }

        public void TurnShieldOnOff(long _characterEntityId, bool _activate, bool _isManualEmitter)
        {
            m_Logger.Write("> Character [" + _characterEntityId + "] is trying to turn " + (_activate ? "on" : "off") + " " + (_isManualEmitter ? "Manual" : "Auto") + " Emitter..", 2);

            CharacterShieldInfo charInfo = m_CharacterInfos[_characterEntityId];
            if (_isManualEmitter)
            {
                if (charInfo.ManualEmitter != null)
                {
                    charInfo.ManualEmitter.IsTurnedOn = _activate;
                    m_Logger.WriteInline("success\n", 2);
                }
                else
                {
                    m_Logger.WriteInline("failed (character doesn't have Manual Emitter)\n", 2);
                }
            } else
            {
                if (charInfo.AutoEmitter != null)
                {
                    charInfo.AutoEmitter.IsTurnedOn = _activate;
                    m_Logger.WriteInline("success\n", 2);
                }
                else
                {
                    m_Logger.WriteInline("failed (character doesn't have Auto Emitter)\n", 2);
                }
            }
        }
        
        public bool RegisterEmitter(List<object> _data)
        {
            PocketShieldAPIV2.ShieldEmitterProperties prop = new PocketShieldAPIV2.ShieldEmitterProperties(_data);
            if (prop.SubtypeId == MyStringHash.NullOrEmpty)
                return false;

            m_EmitterProperties[prop.SubtypeId] = prop;
            return true;
        }

        public PocketShieldAPIV2.ShieldEmitterProperties GetEmitterProperties(MyStringHash _subtypeId)
        {
            if (!m_EmitterProperties.ContainsKey(_subtypeId))
                return null;

            return m_EmitterProperties[_subtypeId];
        }



    }




#if false

    public partial class Session_PocketShieldCoreServer
    {
        private Dictionary<long, CharacterShieldManager> m_ShieldManager_CharacterShieldManagers = null;
        private Dictionary<MyStringHash, List<object>> m_ShieldManager_EmitterConstructionData = null;
        private Dictionary<long, MyTuple<Logger, Logger>> m_ShieldManager_ShieldLoggers = null;

        private PocketShieldAPIV2.ShieldEmitterProperties m_CachedProps = null;

        private ShieldEmitter ShieldManager_CreateEmitter(IMyCharacter _character, MyStringHash _subtypeId)
        {
            if (!m_ShieldManager_EmitterConstructionData.ContainsKey(_subtypeId))
            {
                m_Logger.WriteLine("Emitter " + _subtypeId.String + " is not registered", 2);
                return null;
            }

            PocketShieldAPIV2.ShieldEmitterProperties prop = new PocketShieldAPIV2.ShieldEmitterProperties(m_ShieldManager_EmitterConstructionData[_subtypeId]);
            Logger logger;
            if (prop.IsManual)
                logger = ShieldManager_GetManualShieldLogger(_character);
            else
                logger = ShieldManager_GetAutoShieldLogger(_character);

            m_Logger.WriteLine("DefcOUNT = " + prop.BaseDef.Count);

            return new ShieldEmitter(prop, _character, logger);
        }

        private ShieldEmitter ShieldManager_AddNewEmitter(IMyCharacter _character, MyStringHash _subtypeId, bool _isManualEmitter)
        {
            m_Logger.WriteLine("> Adding new Emitter " + _subtypeId.String + "..", 4);
            ShieldEmitter emitter = ShieldManager_CreateEmitter(_character, _subtypeId);
            if (emitter == null)
                return null;

            if (!m_ShieldManager_CharacterShieldManagers.ContainsKey(_character.EntityId))
                m_ShieldManager_CharacterShieldManagers[_character.EntityId] = new CharacterShieldManager(_character) { m_Logger = m_Logger };

            if (_isManualEmitter)
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter = emitter;
            else
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter = emitter;

            //if (emitter.IsManual)
            //    emitter.IsActive = false;

            return emitter;
        }

        private ShieldEmitter ShieldManager_ReplaceEmitter(IMyCharacter _character, MyStringHash _subtypeId, bool _isManualEmitter)
        {
            m_Logger.WriteLine("> Replacing with Emitter " + _subtypeId.String + "..", 4);
            ShieldEmitter emitter = ShieldManager_CreateEmitter(_character, _subtypeId);
            if (emitter == null)
                return null;

            if (emitter.IsManual != _isManualEmitter)
            {
                m_Logger.WriteLine("Emitter \"Manual\" properties does not match: Emitter is " + emitter.IsManual + ", parameter is " + _isManualEmitter, 1);
                return null;
            }
            
            if (_isManualEmitter)
            {
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter = emitter;
                Logger logger = ShieldManager_GetManualShieldLogger(_character);
                logger.WriteLine("=====> New Emitter <===== ");
                logger.WriteLine("  " + _subtypeId.String);
                logger.WriteLine("========================= ");
            }
            else
            {
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter = emitter;
                Logger logger = ShieldManager_GetAutoShieldLogger(_character);
                logger.WriteLine("=====> New Emitter <===== ");
                logger.WriteLine("  " + _subtypeId.String);
                logger.WriteLine("========================= ");
            }

            return emitter;
        }

        private void ShieldManager_DropEmitter(IMyCharacter _character, bool _isManualEmitter)
        {
            if (!m_ShieldManager_CharacterShieldManagers.ContainsKey(_character.EntityId))
                return;

            m_Logger.WriteLine("> Dropping Emitter..", 4);
            if (_isManualEmitter)
            {
                if (m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter != null)
                {
                    m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter = null;
                    m_ShieldManager_ShieldLoggers[_character.EntityId].Item1.Close();
                }
            }
            else
            {
                if (m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter != null)
                {
                    m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter = null;
                    m_ShieldManager_ShieldLoggers[_character.EntityId].Item2.Close();
                }
            }

            if (!m_ShieldManager_CharacterShieldManagers[_character.EntityId].HasAnyEmitter)
            {
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].Close();
                m_ShieldManager_CharacterShieldManagers.Remove(_character.EntityId);
                m_ShieldManager_ShieldLoggers.Remove(_character.EntityId);
                m_SaveData.ForceRemoveEntry(_character.EntityId);
            }

            m_ForceSyncPlayers.Add(GetPlayerSteamUid(_character));
        }

        private void ShieldManager_ActivateManualShield(long _entityId, bool _activate)
        {
            m_Logger.Write("> Character [" + _entityId + "] is trying to " + (_activate ? "activate" : "deactivate") + " Manual Emitter..", 2);

            if (m_ShieldManager_CharacterShieldManagers.ContainsKey(_entityId) &&
                m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter != null)
            {
                m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter.IsActive = _activate;
                m_Logger.WriteInline(" success\n", 2);
            }
            else
            {
                m_Logger.WriteInline(" failed (character doesn't have Manual Emitter\n", 2);
            }
        }

        private void ShieldManager_TurnShieldOnOff(long _entityId, bool _activate, bool _isManual)
        {
            m_Logger.Write("> Character [" + _entityId + "] is trying to turn " + (_activate ? "on" : "off") + " " + (_isManual ? "Manual" : "Auto") + " Emitter..", 2);

            if (!m_ShieldManager_CharacterShieldManagers.ContainsKey(_entityId))
            {
                m_Logger.WriteInline(" failed (character doesn't have any Emitter\n", 2);
                return;
            }

            if (_isManual && m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter != null)
            {
                m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter.IsTurnedOn = _activate;
                m_Logger.WriteInline(" success\n", 2);
            }
            else
            {
                m_Logger.WriteInline(" failed (character doesn't have Manual Emitter\n", 2);
            }

            if (_isManual && m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter != null)
            {
                m_ShieldManager_CharacterShieldManagers[_entityId].AutoEmitter.IsTurnedOn = _activate;
                m_Logger.WriteInline(" success\n", 2);
            }
            else
            {
                m_Logger.WriteInline(" failed (character doesn't have Auto Emitter\n", 2);
            }
        }

        private Logger ShieldManager_GetManualShieldLogger(IMyCharacter _character)
        {
            if (!m_ShieldManager_ShieldLoggers.ContainsKey(_character.EntityId))
                m_ShieldManager_ShieldLoggers[_character.EntityId] = new MyTuple<Logger, Logger>();

            var loggers = m_ShieldManager_ShieldLoggers[_character.EntityId];
            if (loggers.Item1 == null)
            {
                string loggerName = "shield_" + _character.EntityId;
                if (!_character.IsBot)
                    loggerName += "_" + Utils.GetCharacterName(_character);
                loggerName += "_Manual";

                loggers.Item1 = new Logger(loggerName)
                {
                    LogLevel = m_Config.LogLevel,
                    Suppressed = m_Config.SuppressAllShieldLog
                };
                m_ShieldManager_ShieldLoggers[_character.EntityId] = loggers;
            }

            return loggers.Item1;
        }

        private Logger ShieldManager_GetAutoShieldLogger(IMyCharacter _character)
        {
            if (!m_ShieldManager_ShieldLoggers.ContainsKey(_character.EntityId))
                m_ShieldManager_ShieldLoggers[_character.EntityId] = new MyTuple<Logger, Logger>();

            var loggers = m_ShieldManager_ShieldLoggers[_character.EntityId];
            if (loggers.Item2 == null)
            {
                string loggerName = "shield_" + _character.EntityId;
                if (!_character.IsBot)
                    loggerName += "_" + Utils.GetCharacterName(_character);
                loggerName += "_Auto";

                loggers.Item2 = new Logger(loggerName)
                {
                    LogLevel = m_Config.LogLevel,
                    Suppressed = m_Config.SuppressAllShieldLog
                };
                m_ShieldManager_ShieldLoggers[_character.EntityId] = loggers;
            }

            return loggers.Item2;
        }
        

    }

    public class CharacterShieldManager
    {
        public bool HasAnyEmitter { get { return ManualEmitter != null || AutoEmitter != null; } }
        public bool RequireSync
        {
            get
            {
                if (ManualEmitter != null && ManualEmitter.RequireSync)
                    return true;
                if (AutoEmitter != null && AutoEmitter.RequireSync)
                    return true;

                return false;
            }
        }
        public Vector3 Position { get { return Character.WorldVolume.Center; } }


        public IMyCharacter Character { get; private set; } = null;
        internal PSProjectileDetector ProjectileDetector { get; private set; } = null;
        
        public float LastAutoEnergy { get; private set; } = 0.0f;
        public bool LastAutoTurnedOn { get; private set; } = false;

        public ShieldEmitter ManualEmitter = null;
        public ShieldEmitter AutoEmitter = null;


        public int ManualEmitterIndex = -1;
        public int AutoEmitterIndex = -1;

        public Logger m_Logger = null;

        public CharacterShieldManager(IMyCharacter _character)
        {
            Character = _character;

            ProjectileDetector = new PSProjectileDetector(this, (float)_character.WorldVolume.Radius);
            ProjectileDetector.m_Logger = m_Logger;
            //MyAPIGateway.Projectiles.AddHitDetector(ProjectileDetector);
        }

        public void Close()
        {
            //MyAPIGateway.Projectiles.RemoveHitDetector(ProjectileDetector);
            


        }




        public void Update(int _skipTicks)
        {
            if (ManualEmitter != null)
            {
                ManualEmitter.Update(_skipTicks);
            }

            if (AutoEmitter != null)
            {
                AutoEmitter.Update(_skipTicks);
                LastAutoEnergy = AutoEmitter.Energy;
                LastAutoTurnedOn = AutoEmitter.IsTurnedOn;
            }


            ProjectileDetector.Update();



        }

    }

#endif
}
