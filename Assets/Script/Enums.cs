using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Enums
{
    public enum Direction {
        None = 0x0,
        Forward = 0x1,
        Right = 0x2,
        Backward = 0x4,
        Left = 0x8
    }

    public enum ControlMode {
        Manual,
        Algorithm,
        NeuralTraining,
        Neural
    }

}
