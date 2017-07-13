using UnityEngine;
using Battlehub.Utils;
using System.Collections.Generic;
using System.Linq;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTSaveLoad
{
    [ExecuteInEditMode]
    public class ResourceMap : BundleResourceMap
    {
        [SerializeField]
        [ReadOnly]
        private int m_counter = 1;

        public int GetCounter()
        {
            return m_counter;
        }

        public int IncCounter()
        {
            m_counter++;

            return m_counter;
        }
    }

    public static class ObjectExt
    {
        public static bool HasMappedInstanceID(this Object obj)
        {
            if (IdentifiersMap.Instance == null)
            {
                Debug.LogError("Create Resource Map");
            }
            return IdentifiersMap.Instance.IsResource(obj);
        }

        public static long GetMappedInstanceID(this Object obj)
        {
            if (IdentifiersMap.Instance == null)
            {
                Debug.LogError("Create Resource Map");
            }

            return IdentifiersMap.Instance.GetMappedInstanceID(obj);
        }


        public static long[] GetMappedInstanceID(this Object[] objs)
        {
            if (IdentifiersMap.Instance == null)
            {
                Debug.LogError("Create Resource Map");
            }
            return IdentifiersMap.Instance.GetMappedInstanceID(objs);
        }
    }


    public class IdentifiersMap
    {
        public const string ResourceMapPrefabName = "ResourceMap";

        public const long T_NULL = 1L << 32;
        private const long T_RESOURCE = 1L << 33;
        private const long T_OBJECT = 1L << 34;
        private const long T_DYNAMIC_RESOURCE = 1L << 35;

        private Dictionary<int, int> m_idToDynamicID = new Dictionary<int, int>();
        private Dictionary<int, int> m_dynamicIDToID = new Dictionary<int, int>();
        private Dictionary<int, int> m_idToId = new Dictionary<int, int>();
        private Dictionary<System.Guid, int[]> m_loadedBundles = new Dictionary<System.Guid, int[]>();

        private static IdentifiersMap m_instance;
        public static bool IsInitialized
        {
            get { return m_instance != null; }
        }

        public static IdentifiersMap Instance
        {
            get
            {
                if (m_instance == null)
                {  
                    IdentifiersMap resourceMap = new IdentifiersMap();
                    resourceMap.Initialize();
                }

                return m_instance;
            }
            set
            {
                m_instance = value;
            }
        }

        #if UNITY_EDITOR
        public static void Internal_Initialize(BundleResourceMap bundleResourceMap)
        {
            m_instance = null;
            IdentifiersMap resourceMap = new IdentifiersMap();
            resourceMap.Initialize(bundleResourceMap);
        }
        #endif

        public bool IsResource(Object obj)
        {
            return m_idToId.ContainsKey(obj.GetInstanceID());
        }

        public bool IsDynamicResource(Object obj)
        {
            return m_idToDynamicID.ContainsKey(obj.GetInstanceID());
        }

        public static long ToDynamicResourceID(int id)
        {
            return T_DYNAMIC_RESOURCE | (uint)id;
        }

        public static bool IsNotMapped(Object obj)
        {
            long id = obj.GetMappedInstanceID();
            return (id & T_OBJECT) != 0 || (id & T_NULL) != 0;
        }

        public static bool IsDynamicResourceID(long mappedInstanceID)
        {
            return (mappedInstanceID & T_DYNAMIC_RESOURCE) != 0;
        }

        public long GetMappedInstanceID(Object obj)
        {
            if (obj == null)
            {
                return T_NULL;
            }

            int instanceId = obj.GetInstanceID();
            return GetMappedInstanceID(instanceId);
        }

        public long GetMappedInstanceID(int instanceId)
        {
            long result;
            int mappedInstanceId;
            if (m_idToId.TryGetValue(instanceId, out mappedInstanceId))
            {
                result = T_RESOURCE | (uint)mappedInstanceId;
            }
            else
            {
                if (m_idToDynamicID.TryGetValue(instanceId, out mappedInstanceId))
                {
                    result = T_DYNAMIC_RESOURCE | (uint)mappedInstanceId;
                }
                else
                {
                    result = T_OBJECT | (uint)instanceId;
                }
            }

            return result;
        }

        public long[] GetMappedInstanceID(Object[] obj)
        {
            if (obj == null)
            {
                return null;
            }

            long[] result = new long[obj.Length];
            for (int i = 0; i < obj.Length; ++i)
            {
                Object o = obj[i];
                result[i] = GetMappedInstanceID(o);
            }
            return result;
        }

        public void Register(Object obj, int id)
        {
            if(!m_dynamicIDToID.ContainsKey(id))
            {
                int instanceId = obj.GetInstanceID();
                if (!m_idToDynamicID.ContainsKey(instanceId))
                {
                    m_idToDynamicID.Add(instanceId, id);
                    m_dynamicIDToID.Add(id, instanceId);
                }
            }   
        }

        public void Register(Object obj, long mappedInstanceId)
        {
            if (IsDynamicResourceID(mappedInstanceId))
            {
                int dynamicInstanceId = (int)mappedInstanceId;
                if (!m_dynamicIDToID.ContainsKey(dynamicInstanceId))
                {
                    int instanceId = obj.GetInstanceID();
                    if (!m_idToDynamicID.ContainsKey(instanceId))
                    {
                        m_idToDynamicID.Add(instanceId, dynamicInstanceId);
                        m_dynamicIDToID.Add(dynamicInstanceId, instanceId);
                    }
                }
            }
        }

        public void Unregister(long mappedInstanceId)
        {
            if(IsDynamicResourceID(mappedInstanceId))
            {
                int dynamicInstanceId = (int)mappedInstanceId;
                int instanceId;
                if(m_dynamicIDToID.TryGetValue(dynamicInstanceId, out instanceId))
                {
                    m_idToDynamicID.Remove(instanceId);
                }
                m_dynamicIDToID.Remove(dynamicInstanceId);
            }
        }

        public void Register(AssetBundle bundle)
        {       
            string[] assets = bundle.GetAllAssetNames();
            string resourceMapName = assets.Where(r => r.Contains("resourcemap")).FirstOrDefault();
            GameObject resourceMapGo = bundle.LoadAsset<GameObject>(resourceMapName);
            if (resourceMapGo == null)
            {
                throw new System.ArgumentException(string.Format("Unable to register bundle. Bundle {0} does not contain BundleResourceMap", bundle.name), "bundle");
            }

            BundleResourceMap resourceMap = resourceMapGo.GetComponent<BundleResourceMap>();
            if (resourceMap == null)
            {
                throw new System.ArgumentException(string.Format("Unable to register bundle. Bundle {0} does not contain BundleResourceMap", bundle.name), "bundle");
            }

            System.Guid guid = new System.Guid(resourceMap.Guid);
            if (m_loadedBundles.ContainsKey(guid))
            {
                throw new System.ArgumentException("bundle " + bundle.name + " already loaded", "bundle");
            }

            List<int> ids = new List<int>();
            ResourceGroup[] resourceGroups = resourceMapGo.GetComponentsInChildren<ResourceGroup>(true);
            for(int i = 0; i < resourceGroups.Length; ++i)
            {
                ResourceGroup group = resourceGroups[i];
                LoadMappings(group, false, ids);
            }

            m_loadedBundles.Add(guid, ids.ToArray());
        }

        public void Unregister(AssetBundle bundle)
        {
            string[] assets = bundle.GetAllAssetNames();
            string resourceMapName = assets.Where(r => r.Contains("resourcemap")).FirstOrDefault();
            GameObject resourceMapGo = bundle.LoadAsset<GameObject>(resourceMapName);
            if (resourceMapGo == null)
            {
                throw new System.ArgumentException(string.Format("Unable to unregister bundle. Bundle {0} does not contain BundleResourceMap", bundle.name), "bundle");
            }

            BundleResourceMap resourceMap = resourceMapGo.GetComponent<BundleResourceMap>();
            if (resourceMap == null)
            {
                throw new System.ArgumentException(string.Format("Unable to unregister bundle. Bundle {0} does not contain BundleResourceMap", bundle.name), "bundle");
            }

            System.Guid guid = new System.Guid(resourceMap.Guid);
            if (!m_loadedBundles.ContainsKey(guid))
            {
                return;
            }

            int[] ids = m_loadedBundles[guid];
            for(int i = 0; i < ids.Length; ++i)
            {
                m_idToId.Remove(ids[i]);
            }
            m_loadedBundles.Remove(guid);
        }

        private void LoadMappings(ResourceGroup group, bool ignoreConflicts = false, List<int> ids = null)
        {
            if (!group.gameObject.IsPrefab())
            {
                PersistentIgnore ignore = group.GetComponent<PersistentIgnore>();
                if (ignore == null || ignore.IsRuntime)
                {
                    return;
                }
            }

            ObjectToID[] mappings = group.Mapping;
            for (int j = 0; j < mappings.Length; ++j)
            {
                ObjectToID mapping = mappings[j];
                if(mapping.Object == null)
                {
                    continue;
                }
                
                int realId = mapping.Object.GetInstanceID();
                if(m_idToId.ContainsKey(realId))
                {
                    if(ignoreConflicts)
                    {
                        continue;
                    }
                    Debug.LogError("key " + realId + "already added. Group " + group.name + " guid " + group.Guid + " mapping " + j + " mapped object " + mapping.Object );
                }
                m_idToId.Add(realId, mapping.Id);
                if(ids != null)
                {
                    ids.Add(realId);
                }
            }
        }
        
        private void Initialize()
        {
            BundleResourceMap resourceMap = Resources.Load<ResourceMap>(ResourceMapPrefabName);
            Initialize(resourceMap);
        }

        private void Initialize(BundleResourceMap resourceMap)
        {
            if (resourceMap == null)
            {
                Debug.LogWarning("ResourceMap is null. Create Resource map using Tools->Runtime SaveLoad->Create Resource Map menu item");
                return;
            }
            m_idToId = new Dictionary<int, int>();
            m_instance = this;

            ResourceGroup[] allGroups = Resources.FindObjectsOfTypeAll<ResourceGroup>();
            ResourceGroup[] sceneGroups = allGroups.Where(rg => !rg.gameObject.IsPrefab()).ToArray();

            ResourceGroup[] resourceGroups = resourceMap.GetComponentsInChildren<ResourceGroup>();
            if (resourceGroups.Length == 0)
            {
                Debug.LogWarning("No resource groups found. Create Resource map using Tools->Runtime SaveLoad->Create Resource Map menu item");
                return;
            }

            for (int j = 0; j < resourceGroups.Length; ++j)
            {
                ResourceGroup group = resourceGroups[j];
                bool ignoreConflicts = true;
                LoadMappings(group, ignoreConflicts);
            }


            for (int i = 0; i < sceneGroups.Length; ++i)
            {
                ResourceGroup group = sceneGroups[i];
                LoadMappings(group);
            }
        }

        //1. Find prefabs and other resources;
        public static Dictionary<long, UnityObject> FindResources(bool includeDynamicResources)
        {
            Dictionary<long, UnityObject> objects = new Dictionary<long, UnityObject>();
            UnityObject[] resources = Resources.FindObjectsOfTypeAll<UnityObject>();
            for (int i = 0; i < resources.Length; ++i)
            {
                UnityObject resource = resources[i];
                if (!Instance.IsResource(resource))
                {
                    if (!includeDynamicResources || !Instance.IsDynamicResource(resource))
                    {
                        continue;
                    }
                }

                long instanceId = Instance.GetMappedInstanceID(resource);
                if (instanceId == T_NULL)
                {
                    continue;
                }

                if (objects.ContainsKey(instanceId))
                {
                    Debug.LogErrorFormat("Resource {0}  with instance id {1} already exists {2}", resource, instanceId, objects[instanceId]);
                    continue;
                }

                objects.Add(instanceId, resource);
            }

            return objects;
        }
    }

}

