using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tutinoco
{
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

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(cmds))] private string _cmds;
        public string cmds {
            get { return _cmds; }
            set {
                _cmds = value;
                if( _ignoreJoinSync && Networking.LocalPlayer.isMaster ) _isIgnoredJoinSync = true;
                if( Networking.IsOwner(gameObject) || !_isStandby ) return;
                if( _ignoreJoinSync && !_isIgnoredJoinSync ) { _isIgnoredJoinSync=true; return; }
                ExecuteEvents();
            }
        }

        public void SimpleNetworkInit( bool ignoreJoinSync=false )
        {
            _isInitialized = true;
            _ignoreJoinSync = ignoreJoinSync;
    
            if( _ignoreJoinSync && Networking.IsMaster ) SendEvent("___ignoreJoinSync__");
        }

        public void SendEvent(string name, string value="", bool force=false)
        {
            if( !Networking.IsOwner(gameObject) && !force) return;

            string c = (_isStandby?GenerateCode():cmds+"･")+name+":"+value;
            _isStandby = false;
            cmds = c;

            if( !Networking.IsOwner(gameObject) ) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            else {
                if( _clientSimMode ) SendCustomEvent("RequestExecuteEvnets");
                else RequestSerialization();
            }
        }

        public void RequestExecuteEvnets()
        {
            if( _isStandby ) return;
            ExecuteEvents();
            _isStandby = true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if( Networking.LocalPlayer != player || _isStandby ) return;
            RequestSerialization();
        }

        public override void OnPreSerialization() { RequestExecuteEvnets(); }

        public virtual void ReceiveEvent(string name, string value) { }

        private string GenerateCode() { return ""+((int)UnityEngine.Random.Range(10000000,100000000)); }

        private void ExecuteEvents()
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
        public static bool GetBool( string v ) { return v!="0"; }
        private static string _Bool( bool v ) { return v?"1":"0"; }

        // int
        public void SendEvent(string name, int value, bool force=false) { SendEvent(name, _Int(value), force); }
        public static int GetInt( string v ) { return int.Parse(v); }
        private static string _Int( int v ) { return ""+v; }

        // float
        public void SendEvent(string name, float value, bool force=false) { SendEvent(name, _Float(value), force); }
        public static float GetFloat( string v ) { return float.Parse(v); }
        private static string _Float( float v ) { return ""+v; }

        // Vector3
        public void SendEvent(string name, Vector3 value, bool force=false) { SendEvent(name, _Vector3(value), force); }
        public static Vector3 GetVector3( string v ) { string[] d=v.Split(','); return new Vector3(float.Parse(d[0]), float.Parse(d[1]), float.Parse(d[2])); }
        private static string _Vector3( Vector3 v ) { return v.x+","+v.y+","+v.z; }
    }
}
