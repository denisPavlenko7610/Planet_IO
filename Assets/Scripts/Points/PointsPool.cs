﻿using UnityEngine.Pool;

namespace Planet_IO
{
    public sealed class PointsPool : ObjectPool.ObjectPool<Point>
    {
        public override void Init() =>
            Pool = new ObjectPool<Point>(OnCreate, OnGet, OnRelease, Destroy, false,
                Count, Count + Count);
    }
}