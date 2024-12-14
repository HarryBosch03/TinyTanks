using System.Collections.Generic;
using System.Linq;
using TinyTanks.CDM;
using TinyTanks.Health;
using TMPro;
using UnityEngine;

namespace TinyTanks.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class DamageFeed : MonoBehaviour
    {
        public float entryLifetime;

        private float addTimer;
        
        private CdmController cdmController;
        private TMP_Text text;
        private List<FeedEntry> feed = new();
        private Queue<FeedEntry> queue = new();

        private void Awake()
        {
            cdmController = GetComponentInParent<CdmController>();
            text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            ICanBeDamaged.DamagedEvent += OnDamaged;
        }

        private void OnDisable()
        {
            ICanBeDamaged.DamagedEvent += OnDamaged;
        }

        private void Update()
        {
            for (var i = 0; i < feed.Count; i++)
            {
                feed[i].age += Time.deltaTime;
            }
            feed.RemoveAll(e => e.age > entryLifetime);

            text.text = string.Empty;
            for (var i = 0; i < feed.Count; i++)
            {
                var size = Mathf.InverseLerp(0.2f, 0f, feed[i].age) + 1f;
                text.text += $"<size={size * 100f:0}%>{feed[i].text}</size>\n";
            }

            if (addTimer < 0.2f)
            {
                addTimer += Time.deltaTime;
            }
            else if (queue.Count > 0)
            {
                feed.Add(queue.Dequeue());
            }
        }

        private void OnDamaged(GameObject victim, DamageInstance damage, DamageSource source, ICanBeDamaged.DamageReport report)
        {
            feed.Clear();
            queue.Clear();
            
            if (source.invoker.TryGet(out var invoker))
            {
                if (!report.canPenetrate)
                {
                    queue.Enqueue(new FeedEntry("No Penetration"));
                }
                else if (report.didRicochet)
                {
                    queue.Enqueue(new FeedEntry("Ricochet"));
                }
                else
                {
                    var componentCounts = new Dictionary<CdmComponent, int>();
                    for (var i = 0; i < report.spall.Length; i++)
                    {
                        var spall = report.spall[i];
                        var hitComponent = cdmController.GetComponentFromName(spall.hitComponent);
                        if (!string.IsNullOrEmpty(spall.hitComponent))
                        {
                            if (!componentCounts.TryAdd(hitComponent, 1))
                                componentCounts[hitComponent]++;
                        }
                    }

                    var keys = componentCounts.Keys.ToArray();
                    for (var i = 0; i < keys.Length; i++)
                    {
                        var key = keys[i];
                        if (key.destroyed)
                        {
                            queue.Enqueue(new FeedEntry($"{key.displayName} Destroyed"));
                        }
                        else
                        {
                            queue.Enqueue(new FeedEntry($"Hit {key.displayName} x{componentCounts[key]}"));
                        }
                    }
                }
            }
        }

        private class FeedEntry
        {
            public string text;
            public float age;

            public FeedEntry(string text)
            {
                this.text = text;
                age = 0f;
            }
        }
    }
}