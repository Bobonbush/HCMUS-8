using System;
using System.Collections;
using UnityEngine;


public interface Anomoly
{
    public void Restore();
    public void Evaluate();

    // For custom Evaluate, for example : 
    /*
     * Evaluate on that floor only
     * Evaluate on the top floor (like when player look up there is someone looking down at them)
     * Evaluate all floors like 
     */
    enum EvaluateType
    {
        Global,
        Single
    };

    public EvaluateType getType();
};
