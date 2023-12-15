using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
class Animation {
    static float rotationSpeed = 2f; // TODO attribute?

    /// <summary>
    /// Determine the destination of each sticker
    /// </summary>
    public static List<List<Vector4>> DefineTargets(Puzzle p, SelectSticker selectedSticker, Geometry.Axis begin, Geometry.Axis end) {
        // TODO may be simplified with List<Vector4>?
        List<List<Vector4>> targets = new List<List<Vector4>>(); 
        Matrix4x4 rotate = Geometry.RotationMatrix(begin, end, 90);

        for (int i = 0; i < p.NbCells(); i++) {
            targets.Add(new List<Vector4>());            
            for (int j = 0; j < p.NbStickers(i); j++) {
                if(p.whosGunnaRotate(selectedSticker)[i][j]){
                    targets[i].Add(rotate * p.GetSticker(i, j));
                }else{
                    targets[i].Add(new Vector4());
                }
            }
        }
        return targets;
    }

    /// <summary>
    /// Rotates by 90 degrees with animation
    /// </summary>
    /// <param name="rotationSpeed"> </param>
    public static float RotateOverTime(Puzzle puzzle, GameObject puzzleObject, float totalRotation, List<List<bool>> toBeRotated, Geometry.Axis begin, Geometry.Axis end) {
        // TODO needs optimization? maybe move rotate outside of the function?
        Matrix4x4 rotate = Geometry.RotationMatrix(begin, end, rotationSpeed);
        
        rotationSpeed = Mathf.Clamp(rotationSpeed, 0f, 90f - totalRotation);
        totalRotation = Mathf.Clamp(totalRotation + rotationSpeed, 0f, 90f);
        for (int i = 0; i < puzzleObject.transform.childCount; i++) {
            Transform cell = puzzleObject.transform.GetChild(i);
            for (int j = 0; j < cell.childCount; j++) {
                Transform sticker = cell.GetChild(j);
                if(toBeRotated[i][j]==true){
                    puzzle.setSticker(i, j, rotate * puzzle.GetSticker(i, j));
                    sticker.GetComponent<SelectSticker>().SetCoordinates(puzzle.GetSticker(i,j));
                }
            }
        }
        return totalRotation;
    }
}