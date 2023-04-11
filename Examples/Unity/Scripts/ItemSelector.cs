using System.Collections.Generic;
using UnityEngine;
using VTS.Core;

namespace VTS.Unity.Examples {
    public class ItemSelector : RefreshableDropdown
    {
        public ExamplePlugin _plugin = null;
        private string _itemFileName = "";
        private string _itemInstanceID = "";
        public override void Refresh()
        {
            List<string> itemFiles = new List<string>();
            VTSItemListOptions options = new VTSItemListOptions(){includeAvailableItemFiles = true};
            this._plugin.GetItemList(
                options,
                (s) => {
                    foreach(ItemFile item in s.data.availableItemFiles){
                        itemFiles.Add(item.fileName);
                    }
                    this.RefreshValues(itemFiles);
                },
                (r) => {}
            );
        }

        protected override void SetValue(int index)
        {
            this._itemFileName = this._dropdown.options[index].text;
            Debug.Log(this._itemFileName);
        }

        public void ToggleItemState(bool state){
            if(state){
                VTSItemLoadOptions options = new VTSItemLoadOptions(){
                    positionX = 0,
                    positionY = 0.15f,
                    size = 0.32f,
                    unloadWhenPluginDisconnects = true,
                    order = 1
                };
                this._plugin.LoadItem(
                    _itemFileName,
                    options,
                    (s) => {
                        this._itemInstanceID = s.data.instanceID;
                    },
                    (e) => {
                        Debug.LogError(e.data.message);
                    }
                );
            }else{
                VTSItemUnloadOptions options = new VTSItemUnloadOptions(){
                    fileNames = new string[1]{_itemFileName},
                    unloadAllLoadedByThisPlugin = true
                };
                this._plugin.UnloadItem(
                    options,
                    (s) => {
                        this._itemInstanceID = "";
                    },
                    (e) => {
                        Debug.LogError(e.data.message);
                    }
                );
            }
        }
    
        private void Update(){
            if(this._itemInstanceID.Length > 0){
                float speed = 3f;
                float posX = 0.4f*Mathf.Sin(speed*Time.time);
                float scale = 0.15f + (0.15f*Mathf.Cos(speed*Time.time));
                int order = Mathf.Cos(speed*Time.time) > 0 ? 1 : -1;
                float roation = ((360f/speed)*Time.time)%360;
                VTSItemMoveOptions options = new VTSItemMoveOptions(){
                    positionX = posX,
                    size = scale,
                    order = order,
                    rotation = roation
                };
                VTSItemMoveEntry entry = new VTSItemMoveEntry(
                    this._itemInstanceID,
                    options
                );
                this._plugin.MoveItem(
                    new VTSItemMoveEntry[1]{entry},
                    (s) => {
                        // Debug.Log(JsonUtility.ToJson(s));
                    },
                    (e) => {
                        Debug.LogError(e.data.message);
                    }
                );
            }
        }
    }
}
