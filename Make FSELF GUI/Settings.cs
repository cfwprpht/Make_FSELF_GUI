using System.Configuration;

namespace Make_FSELF_GUI.Properties {    
    internal sealed partial class Settings {        
        public Settings() {}

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public int Mode {
            get { return (int)this["Mode"]; }
            set { this["Mode"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string LastPath {
            get { return (string)this["LastPath"]; }
            set { this["LastPath"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string MfselfPath {
            get { return (string)this["MfselfPath"]; }
            set { this["MfselfPath"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string DbPath {
            get { return (string)this["DbPath"]; }
            set { this["DbPath"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("false")]
        public bool Hexify {
            get { return (bool)this["Hexify"]; }
            set { this["Hexify"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("16")]
        public int HexAllign {
            get { return (int)this["HexAllign"]; }
            set { this["HexAllign"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("1")]
        public int ByteAllign {
            get { return (int)this["ByteAllign"]; }
            set { this["ByteAllign"] = value; }
        }
    }
}
