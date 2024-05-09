using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Matrices {
    public static float[,] GetRowMatrix(float[] vector) {
        if (vector == null) {
            Debug.LogError($"Null vector provided for matrix formation.");
            return null;
        }

        var matrix = new float[1, vector.Length];
        for (var j = 0; j < vector.Length; j++) {
            matrix[0, j] = vector[j];
        }
        return matrix;
    }

    public static float[,] GetColMatrix(float[] vector) {
        if (vector == null) {
            Debug.LogError($"Null vector provided for matrix formation.");
            return null;
        }

        var matrix = new float[vector.Length, 1];
        for (var i = 0; i < vector.Length; i++) {
            matrix[i, 0] = vector[i];
        }
        return matrix;
    }

    public static float[] GetVector(float[,] matrix) {
        if (matrix == null || matrix.GetLength(0) != 1 && matrix.GetLength(1) != 1) {
            Debug.Log($"Invalid matrix provided for vector formation ([{matrix?.GetLength(0) + ""}, {matrix?.GetLength(1) + ""}]).");
            return null;
        }

        float[] result;
        if (matrix.GetLength(0) == 1) {
            result = new float[matrix.GetLength(1)];
            for (var j = 0; j < result.Length; j++) {
                result[j] = matrix[0, j];
            }
        } else {
            result = new float[matrix.GetLength(0)];
            for (var i = 0; i < result.Length; i++) {
                result[i] = matrix[i, 0];
            }
        }
        return result;
    }

    public static float[,] Multiply(float[,] matrix1, float[,] matrix2) {
        if (matrix1 == null || matrix2 == null || matrix1.GetLength(1) != matrix2.GetLength(0)) {
            Debug.LogError($"Invalid matrix dimensions for multiplication: [{matrix1?.GetLength(0) + ""}, {matrix1?.GetLength(1) + ""}]; [{matrix2?.GetLength(0)}, {matrix2?.GetLength(1)}]");
            return null;
        }

        var rows = 1;
        var cols = matrix2.GetLength(1);
        var result = new float[rows, cols];
        for (var i = 0; i < rows; i++) {
            for (var j = 0; j < cols; j++) {
                for (var k = 0; k < matrix1.Length; k++) {
                    result[i, j] += matrix1[i, k] * matrix2[k, j];
                }
            }
        }
        return result;
    }

    public static float[] Multiply(float[] vector, float[,] matrix) {
        if(vector == null || matrix == null) {
            Debug.LogError($"Null arguments provided for multiplication: {vector + ""}; {matrix + ""}");
            return null;
        }

        var matrix1 = GetRowMatrix(vector);
        var resultMatrix = Multiply(matrix1, matrix);
        /*
        Debug.Log($"Multiplication result:");
        foreach (var vectorVal in GetVector(resultMatrix)) {
            Debug.Log($"\t{vectorVal}");
        }
        */
        return GetVector(resultMatrix);
    }

    public static float[] Multiply(float[,] matrix, float[] vector) {
        if (vector == null || matrix == null) {
            Debug.LogError($"Null arguments provided for multiplication: {vector + ""}; {matrix + ""}");
            return null;
        }

        var matrix2 = GetColMatrix(vector);
        var resultMatrix = Multiply(matrix, matrix2);
        return GetVector(resultMatrix);
    }

    public static float[,] Add(float[,] matrix, float scalar) {
        if (matrix == null) {
            Debug.LogError($"Null matrix provided for addition.");
            return null;
        }

        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);
        var result = new float[rows, cols];
        for(var i = 0; i < rows; i++) {
            for(var j = 0; j < cols; j++) {
                result[i, j] = matrix[i, j] + scalar;
            }
        }
        return result;
    }

    public static float[] Add(float[] vector, float scalar) {
        if (vector == null) {
            Debug.LogError($"Null vector provided for addition.");
            return null;
        }

        var matrix = GetRowMatrix(vector);
        var resultMatrix = Add(matrix, scalar);
        return GetVector(resultMatrix);
    }

    public static float Tanh(float value) {
        var eV = Math.Exp(value);
        var eNV = Math.Exp(-value);
        return (float)((eV - eNV) / (eV + eNV));
    }

    public static float[] Tanh(float[] vector) {
        if (vector == null) {
            Debug.LogError($"Null vector provided for Tanh function.");
            return null;
        }

        return vector.Select(v => Tanh(v)).ToArray();
    }

    public static float Sigmoid(float value) {
        return (float)(1 / (1 + Math.Exp(-value)));
    }

    public static float[] Sigmoid(float[] vector) {
        if (vector == null) {
            Debug.LogError($"Null vector provided for Sigmoid function.");
            return null;
        }

        return vector.Select(v => Sigmoid(v)).ToArray();
    }

}
