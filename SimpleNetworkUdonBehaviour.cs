using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tutinoco
{
    public enum Publisher
    {
        All,
        Owner,
        Master,
    }

    public class SimpleNetworkUdonBehaviour : UdonSharpBehaviour
    {
        #if UNITY_EDITOR
        private bool _clientSimMode = true;
        #else
        private bool _clientSimMode = false;
        #endif

        private bool _ignoreJoinSync = false;
        private bool _isStandby = true;
        private bool _isInitialized = false;
        private bool _isIgnoredJoinSync = false;

        private Publisher _publisher = Publisher.All;

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(cmds))] private string _cmds;
        public string cmds {
            get { return _cmds; }
            set {
                _cmds = value;
                if( _ignoreJoinSync && Networking.LocalPlayer.isMaster ) _isIgnoredJoinSync = true;
                if( Networking.IsOwner(gameObject) || !_isStandby ) return;
                if( _ignoreJoinSync && !_isIgnoredJoinSync ) { _isIgnoredJoinSync=true; return; }
                _Receives();
            }
        }

        public void SimpleNetworkInit( Publisher publisher, bool ignoreJoinSync=true )
        {
            _isInitialized = true;
            _publisher = publisher;
            _ignoreJoinSync = ignoreJoinSync;
    
            if( _ignoreJoinSync && Networking.IsMaster ) SendEvent("___ignoreJoinSync__");
        }

        public bool IsPublisher()
        {
            if( _publisher == Publisher.All ) return true;
            if( _publisher == Publisher.Owner && Networking.IsOwner(gameObject) ) return true;
            if( _publisher == Publisher.Master && Networking.IsMaster ) return true;
            return false;
        }

        public void SendEvent(string name, string value="", bool force=false)
        {
            if( !IsPublisher() && !force ) return;

            string c = (_isStandby?_GenerateCode():cmds+"･")+name+":"+value;
            _isStandby = false;
            cmds = c;

            if( !Networking.IsOwner(gameObject) ) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            else {
                if( _clientSimMode ) SendCustomEvent("_RequestReceives");
                else RequestSerialization();
            }
        }

        public void _RequestReceives()
        {
            if( _isStandby ) return;
            _Receives();
            _isStandby = true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if( Networking.LocalPlayer != player || _isStandby ) return;
            RequestSerialization();
        }

        public override void OnPreSerialization() { _RequestReceives(); }

        public void ExecEvent(string name, string value="") { ReceiveEvent(name, value); }

        public virtual void ReceiveEvent(string name, string value) { }

        private string _GenerateCode() { return ""+((int)UnityEngine.Random.Range(10000000,100000000)); }

        private void _Receives()
        {
            if( !_isInitialized ) {
                Debug.LogWarning("Reception was rejected because SimpleNetworkUdonBehaviour has not been initialized. The received data is as follows: "+cmds);
                return;
            }

            string[] data = cmds.Substring(8).Split('･');
            foreach( string cmd in data ) {
                int find = cmd.IndexOf(":");
                string name = cmd.Substring(0,find);
                if( name == "___ignoreJoinSync__" ) return;
                string value = cmd.Substring(find+1);
                ReceiveEvent(name, value);
            }
        }
        
        // bool
        public void SendEvent(string name, bool value, bool force=false) { SendEvent(name, _Bool(value), force); }
        public void ExecEvent(string name, bool value) { ExecEvent(name, _Bool(value)); }
        public static bool GetBool( string v ) { return v!="0"; }
        private static string _Bool( bool v ) { return v?"1":"0"; }

        // int
        public void SendEvent(string name, int value, bool force=false) { SendEvent(name, _Int(value), force); }
        public void ExecEvent(string name, int value) { ExecEvent(name, _Int(value)); }
        public static int GetInt( string v ) { return int.Parse(v); }
        private static string _Int( int v ) { return ""+v; }

        // float
        public void SendEvent(string name, float value, bool force=false) { SendEvent(name, _Float(value), force); }
        public void ExecEvent(string name, float value) { ExecEvent(name, _Float(value)); }
        public static float GetFloat( string v ) { return float.Parse(v); }
        private static string _Float( float v ) { return ""+v; }

        // Vector3
        public void SendEvent(string name, Vector3 value, bool force=false) { SendEvent(name, _Vector3(value), force); }
        public void ExecEvent(string name, Vector3 value) { ExecEvent(name, _Vector3(value)); }
        public static Vector3 GetVector3( string v ) { string[] d=v.Split(','); return new Vector3(float.Parse(d[0]), float.Parse(d[1]), float.Parse(d[2])); }
        private static string _Vector3( Vector3 v ) { return v.x+","+v.y+","+v.z; }
    }
}
