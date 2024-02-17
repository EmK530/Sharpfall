using System.Threading.Tasks;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using TMPro;
using Unity.Physics;
using Unity.Mathematics;
using static UnityEngine.EventSystems.EventTrigger;
using Unity.Rendering;
using Unity.Entities.UniversalDelegates;
using Unity.Burst;

public struct Note
{
    public byte key;
    public ushort track;
    public byte ch;
    public double t;
}

public class NoteManager : MonoBehaviour
{
    public GameObject noteCopy;

    private Entity _cubeEntity;
    private EntityManager _entitymanager;
    private BlobAssetStore _blobAssetStore;
    private GameObjectConversionSettings _settings;

    public static int colorMode = 0;
    public static int colorSet = 0;
    public static bool showStats = true;

    public GameObject colorMode1;
    public GameObject colorMode2;

    static Unity.Mathematics.Random rnd = new Unity.Mathematics.Random((uint)DateTime.Now.Millisecond);
    public TextMeshProUGUI t1;
    public TextMeshProUGUI t2;
    public TextMeshProUGUI t3;
    public TextMeshProUGUI t4;
    double lastTime = 0d;
    static UnityEngine.Material mat;
    public List<GameObject> blocks = new List<GameObject>();
    public List<Entity> entities = new List<Entity>();
    public List<Note> noteQueue = new List<Note>();

    private static List<UnityEngine.Material>[] allSets = new List<UnityEngine.Material>[3];

    int count = 0;
    static int maxBlocks = 1000;
    public static int maxBlocksTemp = 2000;
    int lastQueueSize = 0;
    int nextBlock = 0;
    int maxNotesPerFrame = 8;
    float spawnvel = -5f;
    public static int maxNotesPerFrameTemp = 8;
    long receivedNotes = 0;
    long spawnedNotes = 0;
    bool firstrun = true;
    float offset = 0f;
    static bool stopped = false;
    public static bool AccurateSpawn = true;
    int[] li = new int[256];
    public static bool ready = false;

    static float lastRand = -5f;
    static float UniqueRand(float start, float end, float distRequirement)
    {
        float output = rnd.NextFloat(0f, 1f);
        while (Mathf.Abs(output - lastRand) < distRequirement)
        {
            output = rnd.NextFloat(0f, 1f);
        }
        lastRand = output;
        return output;
    }

    public static void ToggleStats(bool i)
    {
        showStats = i;
    }

    void Prepare()
    {
        blocks.Clear();
        entities.Clear();
        float start = Time.realtimeSinceStartup;
        ready = true;
        return;
    }

    void OnApplicationQuit()
    {
        stopped = true;
        blocks.Clear();
        noteQueue.Clear();
        entities.Clear();
        foreach(List<UnityEngine.Material> i in allSets)
        {
            i.Clear();
        }
        for(int i = 0; i < MIDI.realTracks; i++)
        {
            MIDI.tracks[i] = null;
        }
        MIDI.tracks.Clear();
        GC.Collect();
        Sound.Close();
    }
    public static void LoadMats()
    {
        mat = Resources.Load<UnityEngine.Material>("Note");
        foreach (List<UnityEngine.Material> i in allSets)
        {
            i.Clear();
        }
        for (int i = 0; i < (colorSet==0 ? (colorMode == 0 ? MIDI.realTracks : 16) : 128); i++)
        {
            UnityEngine.Material temp = Instantiate(mat);
            switch (colorSet)
            {
                case 0:
                    temp.color = Color.HSVToRGB(UniqueRand(0f, 1f, 0.15f), 1f, 1f);
                    break;
                case 1:
                    temp.color = Color.HSVToRGB((float)i / 128f, 1f, 1f);
                    break;
                case 2:
                    var n = i % 12;
                    temp.color = (n == 1 || n == 3 || n == 6 || n == 8 || n == 10) ? Color.black : Color.white;
                    break;
            }
            allSets[colorSet].Add(temp);
        }
    }

    public void SetColorMode(int mode)
    {
        colorMode = mode;
        LoadMats();
    }

    public void SetColorSet(int mode)
    {
        colorSet = mode;
        colorMode1.SetActive(colorSet == 0);
        colorMode2.SetActive(colorSet == 0);
        LoadMats();
    }

    void Start()
    {
        for(int i = 0; i < allSets.Length; i++)
        {
            allSets[i] = new List<UnityEngine.Material>();
        }
        _entitymanager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _blobAssetStore = new BlobAssetStore();
        _settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, _blobAssetStore);
        _cubeEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(noteCopy, _settings);
        Prepare();
    }
    [BurstCompile]
    public void MakeNote(ushort track, byte key, byte ch, double delay)
    {
        receivedNotes++;
        if(key <= 127)
        {
            if (li[key] < maxNotesPerFrame)
            {
                li[key] += 1;
                if (count == maxBlocks)
                {
                    noteQueue.RemoveAt(0);
                    count--;
                }
                Note n = new Note();
                n.key = key;
                n.track = track;
                n.ch = ch;
                n.t = delay;
                noteQueue.Add(n);
                count++;
                return;
            }
        }
    }
    public void ClearEntities()
    {
        foreach(Entity i in entities)
        {
            _entitymanager.DestroyEntity(i);
        }
        entities.Clear();
        nextBlock = 0;
        maxBlocks = maxBlocksTemp;
        spawnedNotes = 0;
        receivedNotes = 0;
        offset = Time.realtimeSinceStartup;
        maxNotesPerFrame = maxNotesPerFrameTemp;
    }
    public void SetMKNEF(string txt)
    {
        int num;
        if(int.TryParse(txt,out num))
        {
            maxNotesPerFrameTemp = num;
        }
    }
    public void SetMaxBlocks(string txt)
    {
        int num;
        if (int.TryParse(txt, out num))
        {
            maxBlocksTemp = num;
        }
    }
    public void SetVel(string txt)
    {
        txt = txt.Replace(".", ",");
        float num;
        if (float.TryParse(txt, out num))
        {
            spawnvel = num;
        }
    }
    public void SetAccuSpawn(bool val)
    {
        AccurateSpawn = val;
    }
    [BurstCompile]
    public void SpawnUpdate()
    {
        int satisfactions = 0;
        lastQueueSize = noteQueue.Count;
        foreach(Note item in noteQueue)
        {
            spawnedNotes++;
            int key = item.key;
            int track = item.track;
            int ch = item.ch;
            //GameObject temp = blocks[nextBlock % maxBlocks];
            int calc = nextBlock % maxBlocks;
            if(calc>=entities.Count)
            {
                Entity tmp = _entitymanager.Instantiate(_cubeEntity);
                _entitymanager.AddComponent<PhysicsVelocity>(tmp);
                _entitymanager.AddComponent<Translation>(tmp);
                _entitymanager.AddComponentData(tmp, new Translation { Value = new Vector3(0, -500, 0) });
                _entitymanager.AddComponentData(tmp, new Rotation { Value = transform.rotation });
                entities.Add(tmp);
            }
            Entity temp2 = entities[calc];
            nextBlock++;
            if (nextBlock < maxBlocks)
            {
                /*
                _entitymanager.DestroyEntity(entities[nextBlock % maxBlocks]);
                entities[nextBlock % maxBlocks] = _entitymanager.Instantiate(_cubeEntity);
                temp2 = entities[nextBlock % maxBlocks];
                //_entitymanager.AddComponentData(temp2, new RenderData { material = Instantiate(mat) });
                _entitymanager.AddComponent<PhysicsVelocity>(temp2);
                _entitymanager.AddComponent<Translation>(temp2);
                _entitymanager.AddComponentData(temp2, new Translation { Value = transform.position });
                _entitymanager.AddComponentData(temp2, new Rotation { Value = transform.rotation });
                //temp.SetActive(true);
                */
            }
            //_entitymanager.GetComponentObject<GameObject>(temp2).GetComponent<Renderer>().material.color = Color.HSVToRGB(key / 127f, 1f, 1f);
            //temp.GetComponent<Renderer>().material.color = Color.HSVToRGB(key / 127f, 1f, 1f);
            Translation translation = _entitymanager.GetComponentData<Translation>(temp2);
            PhysicsVelocity velocity = _entitymanager.GetComponentData<PhysicsVelocity>(temp2);
            if (AccurateSpawn)
            {
                float spawnheight = (float)(7f+spawnvel*item.t+(Physics.gravity.y/2*Mathf.Pow((float)item.t,2)));
                float vel = (float)(spawnvel + Physics.gravity.y * item.t);
                translation.Value = new float3(key * 0.1f + (-6.5f), spawnheight, 0f);
                velocity.Linear = new float3(0f, vel, 0f);
            }
            else
            {
                translation.Value = new float3(key * 0.1f + (-6.5f), 7f, 0f);
                velocity.Linear = new float3(0f, spawnvel, 0f);
            }
            velocity.Angular = float3.zero;
            if (_entitymanager.HasComponent<RenderMesh>(temp2))
            {
                RenderMesh temp = _entitymanager.GetSharedComponentData<RenderMesh>(temp2);
                int idx = 0;
                switch (colorSet)
                {
                    case 0:
                        idx = colorMode == 0 ? track : ch;
                        break;
                    case 1:
                    case 2:
                        idx = key;
                        break;
                }
                temp.material = allSets[colorSet][idx];
                _entitymanager.SetSharedComponentData(temp2, temp);
            }
            _entitymanager.SetComponentData(temp2, translation);
            _entitymanager.SetComponentData(temp2, velocity);
            _entitymanager.SetComponentData(temp2, new Rotation { Value = quaternion.identity });
        }
        noteQueue.Clear();
        li = new int[256];
        count = 0;
    }
    void Update()
    {
        if(Time.realtimeSinceStartupAsDouble - lastTime > 5)
        {
            GC.Collect();
        }
        if (showStats)
        {
            t1.gameObject.SetActive(true);
            t2.gameObject.SetActive(true);
            t4.gameObject.SetActive(true);
            t1.text = "FPS: " + 1d / (Time.realtimeSinceStartupAsDouble - lastTime);
            t2.text = "Spawned Notes: " + spawnedNotes + " / " + receivedNotes;
            t3.text = "NoteQueue Size: " + lastQueueSize + " / " + maxBlocks;
            lastTime = Time.realtimeSinceStartupAsDouble;
            float time = Time.realtimeSinceStartup - offset;
            float secs = time;
            float mins = secs / 60;
            secs = (mins - Mathf.Floor(mins)) * 60;
            float hrs = mins / 60;
            mins = (hrs - Mathf.Floor(hrs)) * 60;
            float days = hrs / 24;
            hrs = (days - Mathf.Floor(days)) * 24;
            string daystring = Mathf.Floor(days).ToString();
            string hourstring = Mathf.Floor(hrs).ToString();
            string minstring = Mathf.Floor(mins).ToString();
            string secstring = Mathf.Floor(secs).ToString();
            if (daystring.Length == 1) { daystring = "0" + daystring; }
            if (hourstring.Length == 1) { hourstring = "0" + hourstring; }
            if (minstring.Length == 1) { minstring = "0" + minstring; }
            if (secstring.Length == 1) { secstring = "0" + secstring; }
            t4.text = "Render time: " + daystring + ":" + hourstring + ":" + minstring + ":" + secstring;
        } else
        {
            t1.gameObject.SetActive(false);
            t2.gameObject.SetActive(false);
            t4.gameObject.SetActive(false);
        }
    }
}