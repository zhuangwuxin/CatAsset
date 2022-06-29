﻿using System.Collections.Generic;
using UnityEngine;

namespace CatAsset.Runtime
{
    /// <summary>
    /// 批量资源加载任务完成回调的原型
    /// </summary>
    public delegate void BatchLoadAssetTaskCallback(List<object> assets, object userdata);
    
    /// <summary>
    /// 批量资源加载任务
    /// </summary>
    public class BatchLoadAssetTask : BaseTask<BatchLoadAssetTask>
    {
        private object userdata;
        private List<string> assetNames;
        private BatchLoadAssetTaskCallback onFinished;

        private LoadAssetTaskCallback<Object> onAssetLoadedCallback;
        private int loadedAssetCount;
        private List<object> loadedAssets;
        private List<object> loadSuccessAssets = new List<object>();
        private bool needCancel;

        public BatchLoadAssetTask()
        {
            onAssetLoadedCallback = OnAssetLoaded;
        }

       

        public override void Run()
        {
            State = TaskState.Waiting;
            foreach (string assetName in assetNames)
            {
                CatAssetManager.LoadAsset(assetName, null, onAssetLoadedCallback);
            }
        }

        public override void Update()
        {
            
        }

        public override void Cancel()
        {
            needCancel = true;
        }
        
        /// <summary>
        /// 资源加载结束的回调
        /// </summary>
        private void OnAssetLoaded(bool success, Object asset, object userdata)
        {
            loadedAssetCount++;
            
            if (success)
            {
                loadSuccessAssets.Add(asset);
            }
            
            if (loadedAssetCount != assetNames.Count)
            {
                //资源还未加载完
                State = TaskState.Waiting;
                return;
            }
            
            //全部都加载完了
            State = TaskState.Finished;
            
            //保证资源顺序和加载顺序一致
            foreach (string assetName in assetNames)
            {
                AssetRuntimeInfo assetRuntimeInfo = CatAssetManager.GetAssetRuntimeInfo(assetName); 
                loadedAssets.Add(assetRuntimeInfo.Asset);
            }

            //无需处理已合并任务 因为按照现在的设计 批量加载任务，就算是相同的资源名列表，也是不会判断为重复任务的
            if (!needCancel)
            {
                onFinished?.Invoke(loadedAssets,userdata);
            }
            else
            {
                //被取消了
                foreach (object loadSuccessAsset in loadSuccessAssets)
                {
                    CatAssetManager.UnloadAsset(loadSuccessAsset);
                }
            }
        }


        
        /// <summary>
        /// 创建批量资源加载任务的对象
        /// </summary>
        public static BatchLoadAssetTask Create(TaskRunner owner, string name, List<string> assetNames,object userdata, 
            BatchLoadAssetTaskCallback callback)
        {
            BatchLoadAssetTask task = ReferencePool.Get<BatchLoadAssetTask>();
            task.CreateBase(owner,name);
            task.userdata = userdata;
            task.assetNames = assetNames;
            task.loadedAssets = new List<object>();
            task.onFinished = callback;
            
            return task;
        }

        public override void Clear()
        {
            base.Clear();

            assetNames = default;
            userdata = default;
            onFinished = default;

            loadedAssets = default;
            loadedAssetCount = default;
            loadSuccessAssets.Clear();
            needCancel = default;
            
        }
    }
}