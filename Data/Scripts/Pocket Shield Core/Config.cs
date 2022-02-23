// ;
using ExShared;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace PocketShieldCore
{
    public enum NpcInventoryOperation
    {
        NoTouch = 1 << 0,
        RemoveEmitterOnly = 1 << 1,
        RemovePluginOnly = 1 << 2,
        RemoveEmitterAndPlugin = RemoveEmitterOnly | RemovePluginOnly,
    }

    internal static class Constants
    {
        public const ulong MOD_ID = 2656470280UL;
        public const string LOG_PREFIX = "PocketShieldCore";
        public const string API_BACKEND_VERSION = "1";

        #region Server/Client Default Config
        public const ushort SYNC_ID_TO_CLIENT = 13512;
        public const ushort SYNC_ID_TO_SERVER = 13513;
        public const byte TOGGLE_SHIELD_KEY = 135;

        public const string SERVER_CONFIG_VERSION = "2";
        public const int SERVER_LOG_LEVEL = 1;
        public const int SERVER_UPDATE_INTERVAL = 6; // Server will update 10ups;
        public const int SHIELD_UPDATE_INTERVAL = 3; // Shield will update 20ups;

        public const string CLIENT_CONFIG_VERSION = "1";
        public const int CLIENT_LOG_LEVEL = 1;
        public const int CLIENT_UPDATE_INTERVAL = 10; // Client will update 6ups;
        #endregion

        #region Hud Config
        public const bool SHOW_PANEL = true;
        public const bool SHOW_PANEL_BG = true;

        public const float PANEL_POS_X = 20.0f;
        public const float PANEL_POS_Y = 785.0f;

        public const float PANEL_BASE_WIDTH = 265.0f;
        public const float PANEL_BASE_HEIGHT = 265.0f;

        //public const float PANEL_HEIGHT = 240.0f;
        //public const float PADDING = 6.0f;
        public const float MARGIN = 5.0f;
        public const float ITEM_SCALE = 1.0f;


        #endregion
        public const float SHIELD_QUICKCHARGE_POWER_THRESHOLD = 0.95f;

        public const bool SUPPRESS_ALL_SHIELD_LOG = false;
        public const NpcInventoryOperation NPC_DEATH_INVENTORY_OPERATION = NpcInventoryOperation.RemoveEmitterAndPlugin;
        public const float NPC_DEATH_SHIELD_REFUND_RATIO = 0.1f;

        #region Internal Hud Config
        public const int HIT_EFFECT_LIVE_TICKS = 20;
        public const double HIT_EFFECT_SYNC_DISTANCE = 2000.0;
        public const int ICON_ATLAS_W = 4;
        public const int ICON_ATLAS_H = 1;

        public const int ICON_BLANK = 0;
        public const int ICON_SHIELD_0 = 2;

        public const float ICON_ATLAS_UV_SIZE_X = 1.0f / ICON_ATLAS_W;
        public const float ICON_ATLAS_UV_SIZE_Y = 1.0f / ICON_ATLAS_H;

        public const int TEXTURE_BLANK = ICON_BLANK;
        public const int TEXTURE_SHIELD_BAS = 1;
        public const int TEXTURE_SHIELD_ADV = 5;
        public const int TEXTURE_ICON_DEF_KI = 2;
        public const int TEXTURE_ICON_RES_KI = 3;
        public const int TEXTURE_ICON_DEF_EX = 6;
        public const int TEXTURE_ICON_RES_EX = 7;

        /* 
        BG: 80 92 103
        FG: 187 233 246
        AnimatedSegment: 212 251 254 0.7
        */
        #endregion

        #region Strings
        public const string DAMAGETYPE_KI = "Bullet";
        public const string DAMAGETYPE_EX = "Explosion";
        public const string SUBTYPEID_EMITTER_BAS = "PocketShield_EmitterBasic";
        public const string SUBTYPEID_EMITTER_ADV = "PocketShield_EmitterAdvanced";
        public const string SUBTYPEID_PLUGIN_CAP = "PocketShield_PluginCap";
        public const string SUBTYPEID_PLUGIN_DEF_KI = "PocketShield_PluginDefBullet";
        public const string SUBTYPEID_PLUGIN_DEF_EX = "PocketShield_PluginDefExplosion";
        public const string SUBTYPEID_PLUGIN_RES_KI = "PocketShield_PluginResBullet";
        public const string SUBTYPEID_PLUGIN_RES_EX = "PocketShield_PluginResExplosion";
        #endregion

    }

    public class ServerConfig : Config
    {
        protected string c_SectionMisc = "Misc";

        protected string c_NameServerUpdateInterval = "Server Update interval (ticks)";
        protected string c_NameShieldUpdateInterval = "Shield Update interval (ticks)";
        protected string c_NameSuppressAllShieldLog = "Suppress all Shield Logger";
        protected string c_NameNpcInvOpOnDeath = "What to do with NPC's Inventory on their death";
        protected string c_NameNpcShieldItemToCreditRatio = "NPC's Shield Emitter to Credit Conversion ratio";

        protected string c_CommentShieldUpdateInterval = "Careful! This one is for SHIELD update interval. The other is for Server update interval.";

        public int ServerUpdateInterval { get; set; }
        public int ShieldUpdateInterval { get; set; }

        public bool SuppressAllShieldLog { get; set; }
        public NpcInventoryOperation NpcInventoryOperationOnDeath { get; set; }
        public float NpcShieldItemToCreditRatio { get; set; }

        public ServerConfig(string _filename, Logger _logger) : base(_filename, _logger)
        {

        }

        protected override bool Invalidate(MyIni _iniData)
        {
            ConfigVersion = _iniData.Get(c_SectionCommon, c_NameConfigVersion).ToString(Constants.SERVER_CONFIG_VERSION);
            LogLevel = _iniData.Get(c_SectionCommon, c_NameLogLevel).ToInt32(Constants.SERVER_LOG_LEVEL);
            ServerUpdateInterval = _iniData.Get(c_SectionCommon, c_NameServerUpdateInterval).ToInt32(Constants.SERVER_UPDATE_INTERVAL);
            ShieldUpdateInterval = _iniData.Get(c_SectionCommon, c_NameShieldUpdateInterval).ToInt32(Constants.SHIELD_UPDATE_INTERVAL);

            SuppressAllShieldLog = _iniData.Get(c_SectionMisc, c_NameSuppressAllShieldLog).ToBoolean(Constants.SUPPRESS_ALL_SHIELD_LOG);
            try
            {
                NpcInventoryOperationOnDeath = (NpcInventoryOperation)Enum.Parse(
                    typeof(NpcInventoryOperation),
                    _iniData.Get(c_SectionMisc, c_NameNpcInvOpOnDeath).ToString(Constants.NPC_DEATH_INVENTORY_OPERATION.ToString())
                );
            }
            catch (Exception)
            {
                NpcInventoryOperationOnDeath = Constants.NPC_DEATH_INVENTORY_OPERATION;
            }
            NpcShieldItemToCreditRatio = (float)_iniData.Get(c_SectionMisc, c_NameNpcShieldItemToCreditRatio).ToDouble(Constants.NPC_DEATH_SHIELD_REFUND_RATIO);

            if (ServerUpdateInterval <= 0)
                ServerUpdateInterval = Constants.SERVER_UPDATE_INTERVAL;
            if (ShieldUpdateInterval <= 0)
                ShieldUpdateInterval = Constants.SHIELD_UPDATE_INTERVAL;

            if (ConfigVersion != Constants.SERVER_CONFIG_VERSION)
            {
                m_Logger.WriteLine("  Config version mismatch: read " + ConfigVersion + ", newest version " + Constants.SERVER_CONFIG_VERSION);
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void PackIniData(ref MyIni _iniData)
        {
            base.PackIniData(ref _iniData);

            _iniData.Set(c_SectionCommon, c_NameConfigVersion, ConfigVersion);
            _iniData.Set(c_SectionCommon, c_NameLogLevel, LogLevel);
            _iniData.Set(c_SectionCommon, c_NameServerUpdateInterval, ServerUpdateInterval);
            _iniData.Set(c_SectionCommon, c_NameShieldUpdateInterval, ShieldUpdateInterval);

            _iniData.Set(c_SectionMisc, c_NameSuppressAllShieldLog, SuppressAllShieldLog);
            _iniData.Set(c_SectionMisc, c_NameNpcInvOpOnDeath, NpcInventoryOperationOnDeath.ToString());
            _iniData.Set(c_SectionMisc, c_NameNpcShieldItemToCreditRatio, NpcShieldItemToCreditRatio);

            _iniData.SetComment(c_SectionCommon, c_NameShieldUpdateInterval, c_CommentShieldUpdateInterval);
        }

    }

    public class ClientConfig : Config
    {
        protected string c_SectionHudConfig = "HUD Config";

        protected string c_NameClientUpdateInterval = "Client Update interval (ticks)";

        protected string c_NameShowPanel = "Show Panel";
        protected string c_NameShowPanelBG = "Show Panel Background";
        protected string c_NamePanelPos = "Panel Position (top-left point)";
        protected string c_NameItemScale = "Scale";

        protected string c_CommentPanelPos = "Format: \"X, Y\" (without quotation mark)";

        public int ClientUpdateInterval { get; set; }

        public bool ShowPanel { get; set; }
        public bool ShowPanelBackground { get; set; }
        public Vector2D PanelPosition { get; set; }
        public float ItemScale { get; set; }

        public ClientConfig(string _filename, Logger _logger) : base(_filename, _logger)
        {

        }

        protected override bool Invalidate(MyIni _iniData)
        {
            ConfigVersion = _iniData.Get(c_SectionCommon, c_NameConfigVersion).ToString(Constants.CLIENT_CONFIG_VERSION);
            LogLevel = _iniData.Get(c_SectionCommon, c_NameLogLevel).ToInt32(Constants.CLIENT_LOG_LEVEL);
            ClientUpdateInterval = _iniData.Get(c_SectionCommon, c_NameClientUpdateInterval).ToInt32(Constants.CLIENT_UPDATE_INTERVAL);

            ShowPanel = _iniData.Get(c_SectionHudConfig, c_NameShowPanel).ToBoolean(Constants.SHOW_PANEL);
            ShowPanelBackground = _iniData.Get(c_SectionHudConfig, c_NameShowPanelBG).ToBoolean(Constants.SHOW_PANEL_BG);

            PanelPosition = new Vector2D(Constants.PANEL_POS_X, Constants.PANEL_POS_Y);
            string strPanelPosDefault = (int)PanelPosition.X + ", " + (int)PanelPosition.Y;
            string strPanelPos = _iniData.Get(c_SectionHudConfig, c_NamePanelPos).ToString(strPanelPosDefault);
            string[] part = strPanelPos.Split(',');
            if (part.Length == 2)
            {
                int x;
                int y;
                if (int.TryParse(part[0].Trim(), out x) && int.TryParse(part[1].Trim(), out y))
                    PanelPosition = new Vector2D(x, y);
            }

            ItemScale = (float)_iniData.Get(c_SectionHudConfig, c_NameItemScale).ToDouble(Constants.ITEM_SCALE);
            
            if (ClientUpdateInterval <= 0)
                ClientUpdateInterval = Constants.CLIENT_UPDATE_INTERVAL;

            if (ConfigVersion != Constants.CLIENT_CONFIG_VERSION)
            {
                m_Logger.WriteLine("  Config version mismatch: read " + ConfigVersion + ", newest version " + Constants.CLIENT_CONFIG_VERSION);
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void PackIniData(ref MyIni _iniData)
        {
            base.PackIniData(ref _iniData);

            _iniData.Set(c_SectionCommon, c_NameConfigVersion, ConfigVersion);
            _iniData.Set(c_SectionCommon, c_NameLogLevel, LogLevel);
            _iniData.Set(c_SectionCommon, c_NameClientUpdateInterval, ClientUpdateInterval);

            _iniData.Set(c_SectionHudConfig, c_NameShowPanel, ShowPanel);
            _iniData.Set(c_SectionHudConfig, c_NameShowPanelBG, ShowPanelBackground);
            _iniData.Set(c_SectionHudConfig, c_NamePanelPos, (int)PanelPosition.X + ", " + (int)PanelPosition.Y);
            _iniData.Set(c_SectionHudConfig, c_NameItemScale, ItemScale);

            _iniData.SetComment(c_SectionHudConfig, c_NamePanelPos, c_CommentPanelPos);
        }


    }
}
