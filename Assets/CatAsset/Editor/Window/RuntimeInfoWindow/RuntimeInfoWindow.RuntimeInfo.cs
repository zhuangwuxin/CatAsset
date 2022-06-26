﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CatAsset.Runtime;
using Codice.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CatAsset.Editor
{
    public partial class RuntimeInfoWindow
    {
        private bool isInitRuntimeInfoView;

        private Dictionary<string, BundleRuntimeInfo> bundleRuntimeInfoDict;
        private Dictionary<string, AssetRuntimeInfo> assetRuntimeInfoDict;

        private Vector2 runtimeInfo;
        
        private MethodInfo findTextureByTypeMI = typeof(EditorGUIUtility).GetMethod("FindTextureByType", BindingFlags.NonPublic | BindingFlags.Static);
        private object[] paramObjs = new object[1];
        
        /// <summary>
        /// 资源包相对路径->是否展开
        /// </summary>
        private Dictionary<string, bool> foldOutDcit = new Dictionary<string, bool>();

        /// <summary>
        /// 初始化运行时信息界面
        /// </summary>
        private void InitRuntimeInfoView()
        {
            isInitRuntimeInfoView = true;
            bundleRuntimeInfoDict = typeof(CatAssetManager).GetField(nameof(bundleRuntimeInfoDict), BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as Dictionary<string, BundleRuntimeInfo>;
            assetRuntimeInfoDict = typeof(CatAssetManager).GetField(nameof(assetRuntimeInfoDict), BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as Dictionary<string, AssetRuntimeInfo>;

        }

        /// <summary>
        /// 绘制运行时信息界面
        /// </summary>
        private void DrawRuntimeInfoView()
        {
            if (!isInitRuntimeInfoView)
            {
                InitRuntimeInfoView();
            }
            
            bool isAllFoldOutTrue = false;
            bool isAllFoldOutFalse = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全部展开", GUILayout.Width(100)))
                {
                    isAllFoldOutTrue = true;
                }

                if (GUILayout.Button("全部收起", GUILayout.Width(100)))
                {
                    isAllFoldOutFalse = true;
                }
            }

            using (EditorGUILayout.ScrollViewScope sv = new EditorGUILayout.ScrollViewScope(runtimeInfo))
            {
                runtimeInfo = sv.scrollPosition;

                foreach (KeyValuePair<string, BundleRuntimeInfo> item in bundleRuntimeInfoDict)
                {
                    string bundleRelativePath = item.Key;
                    BundleRuntimeInfo bundleRuntimeInfo = item.Value;

                    //只绘制有资源在使用中，或者被其他资源包依赖的资源包
                    if (bundleRuntimeInfo.UsedAssets.Count > 0 || bundleRuntimeInfo.RefBundles.Count > 0)
                    {
                        if (!foldOutDcit.ContainsKey(bundleRelativePath))
                        {
                            foldOutDcit.Add(bundleRelativePath, false);
                        }

                        if (isAllFoldOutTrue)
                        {
                            //点击过全部展开
                            foldOutDcit[bundleRelativePath] = true;
                        }
                        else if (isAllFoldOutFalse)
                        {
                            //点击过全部收起
                            foldOutDcit[bundleRelativePath] = false;
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            foldOutDcit[bundleRelativePath] = EditorGUILayout.Foldout(foldOutDcit[bundleRelativePath], bundleRelativePath);

                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField(Runtime.Util.GetByteLengthDesc(bundleRuntimeInfo.Manifest.Length));
                            EditorGUILayout.LabelField($"RefBundle数量：{bundleRuntimeInfo.RefBundles.Count}");
                            EditorGUILayout.LabelField($"DependencyBundle数量：{bundleRuntimeInfo.DependencyBundles.Count}");
                        }

                        if (foldOutDcit[bundleRelativePath])
                        {
                            // EditorGUILayout.Space();

                            EditorGUILayout.Space();
                            
                            foreach (AssetRuntimeInfo assetRuntimeInfo in bundleRuntimeInfo.UsedAssets)
                            {
                                DrawAssetRuntimeInfo(assetRuntimeInfo);
                            }

                            EditorGUILayout.Space();
                        }
                    }
                }
            }

           
        }

        /// <summary>
        /// 绘制资源运行时信息
        /// </summary>
        private void DrawAssetRuntimeInfo(AssetRuntimeInfo assetRuntimeInfo)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string assetName = assetRuntimeInfo.AssetManifest.Name;
                
                //资源图标
                GUIContent content = new GUIContent();
                Type assetType;
                if (assetRuntimeInfo.Asset != null)
                {
                    assetType = assetRuntimeInfo.Asset.GetType();
                }
                else
                {
                    //场景资源
                    assetType = typeof(SceneAsset);
                }
             
                if (assetType != typeof(Texture2D))
                {
                    paramObjs[0] = assetType;
                    content.image = (Texture2D) findTextureByTypeMI.Invoke(null, paramObjs);
                }
                else
                {
                    content.image = EditorGUIUtility.FindTexture(assetName);
                }
                
                EditorGUILayout.LabelField("", GUILayout.Width(30));
                
                //图标
                EditorGUILayout.LabelField(content, GUILayout.Width(20));

                //资源名
                EditorGUILayout.LabelField(assetName, GUILayout.Width(400));

                //资源类型
                if (assetRuntimeInfo.Asset != null)
                {
                    EditorGUILayout.LabelField(assetRuntimeInfo.Asset.GetType().Name, GUILayout.Width(100));
                }
                else
                {
                    EditorGUILayout.LabelField("Scene", GUILayout.Width(100));
                }

                //文件字节长度
                EditorGUILayout.LabelField(Runtime.Util.GetByteLengthDesc(assetRuntimeInfo.AssetManifest.Length), GUILayout.Width(100));
                
                //引用计数
                EditorGUILayout.LabelField("引用计数：" + assetRuntimeInfo.RefCount, GUILayout.Width(100));

                EditorGUILayout.LabelField("RefAsset数量：" + assetRuntimeInfo.RefAssets.Count, GUILayout.Width(100));
                if (assetRuntimeInfo.RefAssets.Count > 0 && GUILayout.Button("查看RefAsset", GUILayout.Width(100)))
                {
                    RefAssetListWindow.OpenWindow(this,assetRuntimeInfo);
                }
                
                if (GUILayout.Button("选中", GUILayout.Width(100)))
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath(assetName, assetType);
                }
                

            }
        }

        /// <summary>
        ///  通过依赖加载引用了指定资源的资源列表窗口
        /// </summary>
        private class RefAssetListWindow : EditorWindow
        {
            private AssetRuntimeInfo assetRuntimeInfo;
            private RuntimeInfoWindow parent;
            public static void OpenWindow(RuntimeInfoWindow parent, AssetRuntimeInfo assetRuntimeInfo)
            {
                RefAssetListWindow window = CreateWindow<RefAssetListWindow>(nameof(RefAssetListWindow));
                window.ShowPopup();
                window.parent = parent;
                window.assetRuntimeInfo = assetRuntimeInfo;
                

            }
            
            private void OnGUI()
            {
                if (!Application.isPlaying)
                {
                    Close();
                    return;
                }

                EditorGUILayout.LabelField(assetRuntimeInfo.AssetManifest.Name);
                DrawRefAssetList();
            }

            private void DrawRefAssetList()
            {
                foreach (AssetRuntimeInfo assetRuntimeInfo in assetRuntimeInfo.RefAssets)
                {
                    parent.DrawAssetRuntimeInfo(assetRuntimeInfo);
                }
            }

           
        }
    }
}

