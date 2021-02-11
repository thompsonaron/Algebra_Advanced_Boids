using Leopotam.Ecs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishieBoidz : MonoBehaviour
{
    public GameObject boidModel;

    EcsWorld world;
    EcsSystems systems;

    static Vector3 goalPos = Vector3.zero;
    static FishBoidComponent[] allFish = new FishBoidComponent[10];

    static float  rotationSpeed = 4f;
    static float closeFishDistance = 4f;

    bool rotating = false;

    void Start()
    {
        world = new EcsWorld();
        systems = new EcsSystems(world);
        systems
            .Add(new FishBoidzSystem())
            .Init();

#if UNITY_EDITOR
        Leopotam.Ecs.UnityIntegration.EcsWorldObserver.Create(world);
        Leopotam.Ecs.UnityIntegration.EcsSystemsObserver.Create(systems);
#endif

        // spawn boids
        for (int i = 0; i < 10; i++)
        {
            var entity = world.NewEntity();
            ref FishBoidComponent r = ref entity.Set<FishBoidComponent>();
            r.velocity = new Vector3(1f, 1f, 1f);
            r.speed = Random.Range(1f, 2f); ;
            r.go = (Instantiate(boidModel, new Vector3(Random.Range(-10, 10), Random.Range(-5, 5), Random.Range(-5, 5)), Quaternion.identity));
            allFish[i] = r;
        }
    }
    void Update()
    {
        systems.Run();
    }

    public struct FishBoidComponent
    {
        public GameObject go;
        public float speed;
        public Vector3 velocity;
        public bool turning;
    }

    public class FishBoidzSystem : IEcsRunSystem
    {
        EcsFilter<FishBoidComponent> filter;

        Vector3 vcentre = Vector3.zero;
        Vector3 vavoid = Vector3.zero;
        float groupSpeed = 0.1f;

        float dist;
        int groupSize = 0;

        public void Run()
        {
            if (Random.Range(0, 10000) < 50)
            {
                goalPos = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));
            }

            for (int i = 0; i < filter.GetEntitiesCount(); i++)
            {
                //filter.GetEntity(i);
                var fishie = filter.Get1(i);


                if (Vector3.Distance(fishie.go.transform.position, Vector3.zero) >= 5)
                {
                    fishie.turning = true;
                }
                else
                {
                    fishie.turning = false;
                }

                if (fishie.turning)
                {
                    Vector3 direction = Vector3.zero - fishie.go.transform.position;
                    fishie.go.transform.rotation = Quaternion.Slerp(fishie.go.transform.rotation,
                        Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
                    fishie.speed = Random.Range(0.3f, 0.7f);
                }
                else
                {
                    if (Random.Range(0, 5) < 1)
                    {
                        for (int j = 0; j < filter.GetEntitiesCount(); j++)
                        {
                            var otherFishie = filter.Get1(j);
                            if (fishie.go != otherFishie.go)
                            {
                                dist = Vector3.Distance(otherFishie.go.transform.position, fishie.go.transform.position);
                                if (dist <= closeFishDistance)
                                {
                                    vcentre += otherFishie.go.transform.position;
                                    groupSize++;

                                    if (dist < 1.0f)
                                    {
                                        vavoid = vavoid + (fishie.go.transform.position - otherFishie.go.transform.position);
                                    }

                                    groupSpeed += otherFishie.speed;
                                }
                            }

                            if (groupSize > 0)
                            {
                                vcentre = vcentre / groupSize + (goalPos - fishie.go.transform.position);
                                fishie.speed = groupSpeed / groupSize;

                                Vector3 direction = (vcentre + vavoid) - fishie.go.transform.position;
                                if (direction != Vector3.zero)
                                {
                                    fishie.go.transform.rotation = Quaternion.Slerp(fishie.go.transform.rotation,
                                        Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);

                                }
                            }
                        }
                    }
                }
                fishie.go.transform.Translate(0, 0, Time.deltaTime * fishie.speed);
            }
        }
    }
}