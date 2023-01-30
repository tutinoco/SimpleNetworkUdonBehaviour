using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tutinoco
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SimpleNetworkUdonBehaviour : UdonSharpBehaviour
    {
        private bool isStandby = true;
        private bool isIgnoredInitSync = false;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(cmds))] private string _cmds;
        public string cmds {
            get { return _cmds; }
            set {
                _cmds = value;
                if( Networking.LocalPlayer.isMaster ) isIgnoredInitSync = true;
                if( Networking.IsOwner(gameObject) || !isStandby ) return;
                if( !isIgnoredInitSync ) { isIgnoredInitSync=true; return; }
                ExecuteEvents();
            }
        }

        public void SendEvent(string name, string value)
        {
            string c = (isStandby?GenerateCode():cmds+"･")+name+":"+value;
            isStandby = false;
            cmds = c;

            if( Networking.IsOwner(gameObject) ) RequestSerialization();
            else Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if( Networking.LocalPlayer != player || isStandby ) return;
            RequestSerialization();
        }

        public override void OnPreSerialization()
        {
            ExecuteEvents();
            isStandby = true;
        }

        public virtual void ReceiveEvent(string name, string value) { }

        private string GenerateCode() { return ""+((int)UnityEngine.Random.Range(10000000,100000000)); }

        private void ExecuteEvents()
        {
            string[] data = cmds.Substring(8).Split('･');
            foreach( string cmd in data ) {
                string[] ary = cmd.Split(':');
                ReceiveEvent(ary[0], ary[1]);
            }
        }
    }
}