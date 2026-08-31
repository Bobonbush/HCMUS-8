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
        Single,
        // Evaluate runs on the floor above or below the player (GameManager.ForceAnomoly(+1/-1, index)).
        // The anomoly on the player's own floor is only bookkeeping so RestoreAnomoly knows the type.
        AddUp,
        NPCInvolve,
        EntireFloor
    };

    public EvaluateType getType();
};
