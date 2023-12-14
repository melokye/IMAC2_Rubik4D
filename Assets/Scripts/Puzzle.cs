using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle : Attribute {
        // TODO _stickers can be optimized?
    List<List<Vector4>> _stickers = new List<List<Vector4>>();
    const int _nbCells = 8;
    private float stickerDistance = 10f; // TODO -> in renderer ?

    /// <summary>
    /// Generate a 4D Rubik
    /// </summary>
    /// <param name="n">Size of the Rubik (by default it's a 2x2x2x2 Rubik)</param>
    public Puzzle(int n = 2){ 
        // TODO n must be added in parameters -> puzzleSize
        for (int i = 0; i < _nbCells; i++) { // 8 == nbCells
            // Define a cell
            Vector4 cell = Vector4.zero;
            int iCell = Mathf.FloorToInt(i * 0.5f);
            cell[iCell] = 1 - (2 * (i % 2)); // 1 or -1 depending the situation

            // Create the stickers at the i-eme cell
                // TODO explain more?
            _stickers.Add(new List<Vector4>());
            for (int j = 0; j < Mathf.Pow(n, 3); j++) {
                Vector3 temp = new Vector3(0, 0, 0);
                if (n > 1) {
                    temp.x = Mathf.Lerp(-1f, 1f,
                        (Mathf.FloorToInt(j / Mathf.Pow(n, 2)) % n) / (n - 1f));
                    temp.y = Mathf.Lerp(-1f, 1f,
                        (Mathf.FloorToInt(j / n) % n) / (n - 1f));
                    temp.z = Mathf.Lerp(-1f, 1f,
                        (j % n) / (n - 1f));
                }
                Vector4 sticker = new Vector4(0, 0, 0, 0);
                sticker = InsertFloat(temp / stickerDistance, cell[iCell], iCell);
                _stickers[i].Add(sticker);
            }
        }
    }

    /// <summary>
    /// Inserts value in Vector3 at pos, making it a Vector4
    /// // TODO add more details about this function -> coord homogene?
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="value"></param>
    /// <param name="pos"></param>
    /// <returns>Vector4 with value inserted at index pos</returns>
    private Vector4 InsertFloat(Vector3 vec, float value, int pos) {
        pos = Mathf.Clamp(pos, 0, 3);
        Vector4 result = Vector4.zero;
        switch (pos) {
            case 0:
                result = new Vector4(value, vec.x, vec.y, vec.z);
                break;
            case 1:
                result = new Vector4(vec.x, value, vec.y, vec.z);
                break;
            case 2:
                result = new Vector4(vec.x, vec.y, value, vec.z);
                break;
            case 3:
                result = new Vector4(vec.x, vec.y, vec.z, value);
                break;
            default:
                break;
        }
        return result;
    }

    public List<List<Vector4>> GetStickers(){
        return _stickers;
    }

    public int NbCells(){
        return _nbCells;
    }

    public int NbStickers(int index){
        return _stickers[index].Count;
    }

    public Vector4 GetSticker(int iCell, int iSticker){
        return _stickers[iCell][iSticker];
    }

    public void setSticker(int iCell, int iSticker, Vector4 value){
        _stickers[iCell][iSticker] = value;
    }
}