using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NineAxis {
    public string datetime;
    //加速度
    public float ax;
    public float ay;
    public float az;
    //地磁気
    public float lx;
    public float ly;
    public float lz;
    //角速度
    public float gx;
    public float gy;
    public float gz;
}