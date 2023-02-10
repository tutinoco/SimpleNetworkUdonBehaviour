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

    public enum JoinSync
    {
        None,
        Latest,
        Logging,
    }

    public class SimpleNetworkUdonBehaviour : UdonSharpBehaviour
    {
        #if UNITY_EDITOR
        private bool _clientSimMode = true;
        #else
        private bool _clientSimMode = false;
        #endif

        private bool _isStandby = true;
        private bool _isInitialized = false;
        private bool _isIgnoredJoinSync = false;
        private bool _isReceivedJoinSync = false;

        private Publisher _publisher = Publisher.All;

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(cmds))] private string _cmds;
        public string cmds {
            get { return _cmds; }
            set {
                _cmds = value;
                if( Networking.LocalPlayer.isMaster ) _isIgnoredJoinSync = true;
                if( Networking.IsOwner(gameObject) || !_isStandby ) return;
                if( _isIgnoredJoinSync ) _Receives(_cmds);
                _isIgnoredJoinSync = true;
            }
        }

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(jcmds))] private string _jcmds;
        public string jcmds {
            get { return _jcmds; }
            set {
                _jcmds = value;
                if( Networking.LocalPlayer.isMaster ) _isReceivedJoinSync = true;
                if( !_isReceivedJoinSync ) _Receives(_jcmds);
                _isReceivedJoinSync = true;
            }
        }

        public void SimpleNetworkInit() { SimpleNetworkInit( Publisher.All ); } 
        public void SimpleNetworkInit( Publisher publisher )
        {
            _isInitialized = true;
            _publisher = publisher;
    
            if( Networking.IsMaster ) SendEvent("__init__", "", JoinSync.Logging);
        }

        public bool IsPublisher()
        {
            if( _publisher == Publisher.All ) return true;
            if( _publisher == Publisher.Owner && Networking.IsOwner(gameObject) ) return true;
            if( _publisher == Publisher.Master && Networking.IsMaster ) return true;
            return false;
        }

        public void ExecEvent(string name, string value) { ReceiveEvent(name, value); ReceiveEvent(name, value, Networking.LocalPlayer); }
        public void SendEvent(string name, string value, bool force=false) { SendEvent(name, value, JoinSync.None, force); }
        public void SendEvent(string name, string value, JoinSync joinSync, bool force=false)
        {
            if( !IsPublisher() && !force ) return;

            string id = Networking.LocalPlayer.playerId.ToString("d2");
            string c = (_isStandby?_GenerateCode():cmds+"･")+id+name+":"+value;
            _isStandby = false;
            cmds = c;

            if( joinSync == JoinSync.Latest ) ClearJoinSync(name);
            if( joinSync != JoinSync.None ) jcmds = (jcmds!=""?jcmds+"･":"")+id+name+":"+value;

            if( !Networking.IsOwner(gameObject) ) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            else if( _clientSimMode ) SendCustomEvent("_RequestReceives");
            else RequestSerialization();
        }

        public void ClearJoinSync() { jcmds = ""; }
        public void ClearJoinSync( string name )
        {
            string result = "";
            foreach( string cmd in jcmds.Split('･') ) {
                if( name == cmd.Substring(0,cmd.IndexOf(":")) ) continue;
                result += result==""?cmd:'･'+cmd;
            }
            jcmds = result;
        }

        public void _RequestReceives()
        {
            if( _isStandby ) return;
            _Receives(cmds);
            _isStandby = true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if( Networking.LocalPlayer != player || _isStandby ) return;
            RequestSerialization();
        }

        public override void OnPreSerialization() { _RequestReceives(); }

            public virtual void ReceiveEvent(string name, string value) { }
        public virtual void ReceiveEvent(string name, string value, VRCPlayerApi player) { }

        private string _GenerateCode() { return ""+((int)UnityEngine.Random.Range(10000000,100000000)); }

        private void _Receives( string c )
        {
            if( !_isInitialized ) {
                Debug.LogWarning("Reception was rejected because SimpleNetworkUdonBehaviour has not been initialized. The received data is as follows: "+cmds);
                return;
            }

            string[] data = c.Substring(8).Split('･');
            foreach( string cmd in data ) {
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(int.Parse(cmd.Substring(0,2)));
                int find = cmd.IndexOf(":");
                string name = cmd.Substring(2,find-2);
                if( name == "__init__" ) return;
                string value = cmd.Substring(find+1);
                ReceiveEvent(name, value);
                ReceiveEvent(name, value, player);
            }
        }

        // void
        public void ExecEvent(string name) { ExecEvent(name, ""); }
        public void SendEvent(string name) { SendEvent(name, "", JoinSync.None, false); }

        // bool
        public void ExecEvent(string name, bool value) { ExecEvent(name, _Bool(value)); }
        public void SendEvent(string name, bool value, bool force=false) { SendEvent(name, value, JoinSync.None, false); }
        public void SendEvent(string name, bool value, JoinSync joinSync, bool force=false) { SendEvent(name, _Bool(value), joinSync, force); }
        public static bool GetBool( string v ) { return v!="0"; }
        private static string _Bool( bool v ) { return v?"1":"0"; }

        // int
        public void ExecEvent(string name, int value) { ExecEvent(name, _Int(value)); }
        public void SendEvent(string name, int value, bool force=false) { SendEvent(name, value, JoinSync.None, false); }
        public void SendEvent(string name, int value, JoinSync joinSync, bool force=false) { SendEvent(name, _Int(value), joinSync, force); }
        public static int GetInt( string v ) { return int.Parse(v); }
        private static string _Int( int v ) { return ""+v; }

        // float
        public void ExecEvent(string name, float value) { ExecEvent(name, _Float(value)); }
        public void SendEvent(string name, float value, bool force=false) { SendEvent(name, value, JoinSync.None, false); }
        public void SendEvent(string name, float value, JoinSync joinSync, bool force=false) { SendEvent(name, _Float(value), joinSync, force); }
        public static float GetFloat( string v ) { return float.Parse(v); }
        private static string _Float( float v ) { return ""+v; }

        // Vector3
        public void ExecEvent(string name, Vector3 value) { ExecEvent(name, _Vector3(value)); }
        public void SendEvent(string name, Vector3 value, bool force=false) { SendEvent(name, value, JoinSync.None, false); }
        public void SendEvent(string name, Vector3 value, JoinSync joinSync, bool force=false) { SendEvent(name, _Vector3(value), joinSync, force); }
        public static Vector3 GetVector3( string v ) { string[] d=v.Split(','); return new Vector3(float.Parse(d[0]), float.Parse(d[1]), float.Parse(d[2])); }
        private static string _Vector3( Vector3 v ) { return v.x+","+v.y+","+v.z; }
    }
}
