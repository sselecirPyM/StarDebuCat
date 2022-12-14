using StarDebuCat.Attributes;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MilkWangBase
{
    public enum MarkType
    {
        Unknown,
        HeightInvisibleEnemy,

    }
    public class Mark
    {
        public Unit unit;
        public Vector2? position;
        public float life;
        public float lifeTime;
        public Vector2 speed;
        public string name;
    }
    public class MarkerSystem
    {
        AnalysisSystem analysisSystem;
        [Find("ReadyToPlay")]
        bool readyToPlay;

        public HashSet<Mark> marks = new();

        public HashSet<Mark> prepareRemove = new();

        void Update()
        {
            if (!readyToPlay)
                return;


            foreach (var mark in marks)
            {
                //mark.lifeTime += 0.0625f;
                mark.lifeTime += 0.044642857f;
                if (mark.lifeTime > mark.life)
                {
                    prepareRemove.Add(mark);
                }
                else
                {
                }
            }

            foreach (var mark in prepareRemove)
                marks.Remove(mark);
            prepareRemove.Clear();
        }
        public void AddMark(Vector2 position, string name, float time)
        {
            marks.Add(new Mark()
            {
                life = time,
                position = position,
                name = name,
            });
        }

        public bool HitTest(string mark, Vector2 position, float radius)
        {
            return false;
        }
    }
}
