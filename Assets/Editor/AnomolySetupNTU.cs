using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Builds and wires anomolies 10, 12, 13, 18, 22, 24, 26 and 36 into Floor.prefab.
//
// This is the "setup tooling" the other anomolies refer to in their comments -
// the anomoly scripts themselves never look anything up by name, every reference
// has to be dropped into a serialized field, and anomoly 12 alone needs ~26 desk
// transforms. Doing that by hand is neither repeatable nor reviewable, so it
// happens here instead.
//
//   Tools -> Anomoly Setup (NTU)
//
// Safe to run more than once. Objects are looked up by name before they are
// created, list entries are de-duplicated, and anything that already exists keeps
// the transform it has - so if you nudge the notice board into a nicer spot in
// the Scene view, re-running the tool will not shove it back.
public static class AnomolySetupNTU
{

    private const string FloorPrefabPath = "Assets/Prefabs/Floor.prefab";
    private const string ArtPath = "Assets/Art/Banners/";

    // How many extra fixtures anomoly 10 squeezes into each gap. Four puts twenty
    // extra lamps in a corridor built for nine - absurd on sight, which is the point,
    // while still reading as a corridor rather than a light rig.
    private const int ExtraPerGap = 4;

    // The two corridor runs of ceiling lights. Anomoly 10 clones the first of each
    // pair and spaces copies along the gap between them.
    private static readonly string[,] ExtraLightPairs =
    {
        { "f (1)", "f" },
        { "f", "f (2)" },
        { "f (2)", "f (3)" },
        { "f (4)", "f (6)" },
        { "f (6)", "f (7)" },
    };

    // Corridor fixtures anomoly 26 may hang crooked (the lit overhead run, without
    // the low elevator-lobby lamps).
    private static readonly string[] CrookedCandidates =
    {
        "f", "f (1)", "f (2)", "f (3)", "f (4)", "f (5)", "f (6)", "f (7)", "f (8)",
    };

    // Corridor props anomoly 18 swells. Wall-mounted for the most part, so growth
    // cannot shove the player around; the bin is the only one standing on the floor.
    private static readonly string[] GrowingProps =
    {
        "SM_trash_can Variant", "SM_fire_extinguisher", "SM_fire_lever", "SM_switch",
    };

    private static readonly string[] Classrooms =
    {
        "ClassRoom Detail", "ClassRoom Detail (1)", "ClassRoom Detail (2)",
        "ClassRoom Detail (3)", "ClassRoom Detail (4)", "ClassRoom Detail (5)",
    };

    // The room whose desks get heaped against the door: the left-hand one nearest the
    // lifts. It is not the closest room to the spawn point, but it is the one the
    // player actually goes into.
    private const string DeskRoom = "ClassRoom Detail";

    // The map's classroom lamp runs at intensity 1.7 with a 9m range, while the
    // corridor lights it competes with are spots at intensity 25. Cloned at those
    // settings the anomoly fires correctly and is simply imperceptible from the
    // corridor, so the lamps this tool owns are given enough punch to read through a
    // doorway from the far side of a lit corridor. Brighter than the corridor spots on
    // purpose - the room has to visibly announce itself. The map's own lamps are never
    // touched.
    private const float ClonedLampIntensity = 30f;
    private const float ClonedLampRange = 26f;

    // Desks closer than this to a neighbour count as the same classroom. Seats within a
    // room are about 2.1m apart; the nearest stray in the corridor is 3.4m off.
    private const float DeskClusterLink = 3f;

    // The heap of desks: how far it spreads before stacking upward, how tightly a
    // layer packs, how high each layer sits, and how wildly the desks are turned.
    private const float HeapRadius = 1.1f;
    private const float HeapSpacing = 0.55f;
    private const float HeapLayerHeight = 0.7f;
    private const float HeapSpin = 180f;

    // Lamps this tool adds are named apart from the map's own "ClassLight" so a re-run
    // can tell which are its to normalise and which belong to whoever built the map.
    private const string ClonedLampName = "ClassLight (Anomoly13)";

    // The green board out in the corridor by the toilets. It is parented under a
    // classroom rather than the Floor root - a quirk of how it was placed - but it
    // physically stands mid-corridor, about a metre from the urinals.
    private const string CorridorBoardRoom = "ClassRoom Detail";
    private const string CorridorBoardName = "board (1)";

    // Sheet placement, measured from the board's own transform rather than derived
    // from its renderer bounds. Deriving it produced a different depth every time the
    // bounds calculation changed, and the window that actually reads is narrow - 9cm
    // too far and the sheet disappears behind the wall. These are the values that were
    // confirmed by looking at them in the scene, so they are written down, not
    // recomputed. Only the axes below are still derived, so the sheets follow the board
    // if it is ever moved or turned.
    private const float PosterRise = 1.222f;     // up from the board's pivot, which sits at its base
    private const float PosterDepth = 0.023f;    // clear of the board face, toward the corridor
    private const float PosterStep = 1.517f;     // sideways gap between neighbouring sheets
    private const float PosterHeightMetres = 1.52f;

    // Poster texture is 1448x2048, so the quad has to be that shape or the print skews.
    private const float PosterAspect = 1448f / 2048f;

    // Which side of the board the sheets go on, along its thin axis. Established by
    // looking, not by reasoning about the geometry: on the +z side the sheets end up
    // inside the wall. The readable face is -z, so they sit at z about -27.35 facing
    // back down the corridor.
    private const float PosterFaceSign = -1f;

    // The logo plaque hangs on the wall facing the lifts, so the crest is what you
    // look at while waiting and again when the doors open. Placed by hand in the
    // Scene view and recorded here, so a clean rebuild puts it back in the same spot.
    // The tool leaves it alone once it exists, so it stays nudgeable.
    private static readonly Vector3 LogoPosition = new Vector3(20.15f, 3.0f, -4.7f);
    private static readonly Vector3 LogoEuler = new Vector3(0f, 180f, 0f);
    private static readonly Vector3 LogoScale = new Vector3(1.2f, 1.2f, 1f);

    // The eyes on the vision sheet, for anomoly 36. Taken from where they are drawn
    // in the 1448x2048 artwork - lens centres at pixel (410, 1180) and (1038, 1180) -
    // and converted to metres across the printed sheet, so the irises land on the
    // whites without anyone lining them up by eye in the Scene view.
    private const float EyeAcross = 0.233047f;    // out from the sheet's centre line
    private const float EyeRise = -0.115781f;     // down from its middle
    private const float EyeSize = 0.106875f;      // the iris quad: 144px of the artwork

    // How far off the sheet an iris floats, along the sheet's own forward. Established
    // by looking, not by reasoning about which way the board faces - pushing the eyes
    // the other way along that axis hides them behind the paper, which is exactly what
    // happened the first time. Both eyes take the same value: a millimetre of disagreement
    // between them leaves one coplanar with the sheet and flickering.
    private const float EyeDepthOffset = -0.0026f;

    // How far an iris may slide. The lens is an almond, so it pinches towards the
    // corners: at this deflection the iris still clears the drawn lid by a few pixels,
    // and much further than this it pokes straight through it.
    private const float EyeTravelX = 0.052f;
    private const float EyeTravelY = 0.0037f;

    // A printed sheet, in metres.
    private static readonly Vector2 PosterSize =
        new Vector2(PosterHeightMetres * PosterAspect, PosterHeightMetres);


    [MenuItem("Tools/Anomoly Setup (NTU)")]
    public static void Run()
    {
        GameObject floor = PrefabUtility.LoadPrefabContents(FloorPrefabPath);
        if (floor == null)
        {
            Debug.LogError("Anomoly setup: could not open " + FloorPrefabPath);
            return;
        }

        try
        {
            AnomolyManager manager = floor.GetComponent<AnomolyManager>();
            if (manager == null)
            {
                Debug.LogError("Anomoly setup: no AnomolyManager on the Floor root.");
                return;
            }
            if (manager.anomolies == null) manager.anomolies = new List<MonoBehaviour>();

            Register(manager, BuildAnomoly10(floor));
            Register(manager, BuildAnomoly12(floor));
            Register(manager, BuildAnomoly13(floor));
            Register(manager, BuildAnomoly18(floor));
            Register(manager, BuildAnomoly22(floor));
            Register(manager, BuildAnomoly24(floor));
            Register(manager, BuildAnomoly26(floor));
            Register(manager, BuildAnomoly36(floor));

            PrefabUtility.SaveAsPrefabAsset(floor, FloorPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Anomoly setup: done. AnomolyManager now holds " + manager.anomolies.Count + " anomolies.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(floor);
        }
    }


    // ---- the seven anomolies -------------------------------------------------

    private static MonoBehaviour BuildAnomoly10(GameObject floor)
    {
        Anomoly10 anomoly = Host<Anomoly10>(floor, "Anomoly#10");
        anomoly.extraLights = new List<GameObject>();

        int made = 0;
        for (int i = 0; i < ExtraLightPairs.GetLength(0); i++)
        {
            Transform source = floor.transform.Find(ExtraLightPairs[i, 0]);
            Transform other = floor.transform.Find(ExtraLightPairs[i, 1]);
            if (source == null || other == null)
            {
                Debug.LogWarning("Anomoly 10: missing " + ExtraLightPairs[i, 0] + " or " + ExtraLightPairs[i, 1]);
                continue;
            }

            // Space them evenly along the gap instead of one at the midpoint, so the
            // row reads as densely, absurdly over-lit rather than merely doubled up.
            for (int k = 1; k <= ExtraPerGap; k++)
            {
                string cloneName = "ExtraLight_" + made;
                Transform existing = floor.transform.Find(cloneName);
                if (existing == null)
                {
                    GameObject clone = Object.Instantiate(source.gameObject, floor.transform);
                    clone.name = cloneName;
                    existing = clone.transform;
                }

                // Always repositioned - these are derived from the real lights, so a
                // changed count or a moved fixture reshuffles them all correctly.
                existing.localRotation = source.localRotation;
                existing.localScale = source.localScale;
                existing.localPosition = Vector3.Lerp(source.localPosition, other.localPosition,
                                                      k / (float)(ExtraPerGap + 1));
                existing.gameObject.SetActive(false);

                // The clones are decorative infill between real fixtures that already
                // cast the corridor's shadows, so they add light without adding to the
                // shadow atlas. Eight of the twenty are copies of shadow-casting lamps,
                // and switching all twenty on would otherwise push the atlas into
                // halving its resolution. Normalised every run, not just at creation, so
                // a clone somebody edited by hand is repaired.
                Light clonedLamp = existing.GetComponent<Light>();
                if (clonedLamp != null) clonedLamp.shadows = LightShadows.None;

                anomoly.extraLights.Add(existing.gameObject);
                made++;
            }
        }

        Debug.Log("Anomoly 10: " + anomoly.extraLights.Count + " extra lights.");
        return anomoly;
    }

    private static MonoBehaviour BuildAnomoly12(GameObject floor)
    {
        Anomoly12 anomoly = Host<Anomoly12>(floor, "Anomoly#12");
        anomoly.seats = new List<Transform>();

        Transform room = floor.transform.Find(DeskRoom);
        if (room == null)
        {
            Debug.LogWarning("Anomoly 12: classroom " + DeskRoom + " not found.");
            return anomoly;
        }
        List<Transform> allSeats = new List<Transform>();
        foreach (Transform child in room)
        {
            if (child.name.StartsWith("chair1")) allSeats.Add(child);
        }

        // A "ClassRoom Detail" object is not one room. It also holds single-file rows of
        // desks strung out along the corridor, tens of metres from the classroom itself,
        // and averaging all of them put the heap out in the corridor between two rooms.
        // Taking the densest clump of desks gets the actual classroom.
        anomoly.seats = LargestCluster(allSeats, DeskClusterLink);

        // The desks may only be moved within the footprint they already occupy. That
        // footprint is inside the room by definition, so a pile wider than the
        // classroom banks up against its walls instead of ending up in the lift.
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Transform seat in anomoly.seats)
        {
            if (seat == null) continue;
            min = Vector3.Min(min, seat.localPosition);
            max = Vector3.Max(max, seat.localPosition);
        }
        anomoly.areaMin = min;
        anomoly.areaMax = max;

        // The heap centres on the middle of the room's own desks, so the pile sits out
        // in the open floor where it cannot be missed rather than tucked by the door.
        Vector3 pool = room.InverseTransformPoint(DeskCentreWorld(room, anomoly.seats));
        pool.x = Mathf.Clamp(pool.x, min.x, max.x);
        pool.z = Mathf.Clamp(pool.z, min.z, max.z);
        anomoly.poolCentre = pool;

        // Set explicitly, never left to the field defaults. Unity keeps whatever was
        // serialized the first time a component was added, so changing a default in the
        // script does nothing to a prefab that already has one - the tool has to write
        // the values or they stay frozen at their first ever run.
        anomoly.poolRadius = HeapRadius;
        anomoly.spacing = HeapSpacing;
        anomoly.layerHeight = HeapLayerHeight;
        anomoly.spinDegrees = HeapSpin;

        Debug.Log("Anomoly 12: " + anomoly.seats.Count + " of " + allSeats.Count
                  + " desks in " + DeskRoom
                  + ", heaping at the room centre"
                  + " (local " + anomoly.poolCentre + "), kept inside "
                  + min + " .. " + max + ", "
                  + Mathf.Max(1, Mathf.FloorToInt(Mathf.PI * HeapRadius * HeapRadius / (HeapSpacing * HeapSpacing)))
                  + " per layer.");
        return anomoly;
    }

    // The biggest group of desks that are all within reach of a neighbour. Desks in a
    // classroom sit about 2.1m apart; the strays along the corridor are separated from
    // it by more than 3m, so they fall into their own small groups and are dropped.
    private static List<Transform> LargestCluster(List<Transform> seats, float link)
    {
        List<Transform> best = new List<Transform>();
        bool[] seen = new bool[seats.Count];

        for (int i = 0; i < seats.Count; i++)
        {
            if (seen[i] || seats[i] == null) continue;

            List<Transform> group = new List<Transform>();
            Queue<int> pending = new Queue<int>();
            pending.Enqueue(i);
            seen[i] = true;

            while (pending.Count > 0)
            {
                int a = pending.Dequeue();
                group.Add(seats[a]);
                for (int b = 0; b < seats.Count; b++)
                {
                    if (seen[b] || seats[b] == null) continue;
                    if (Vector3.Distance(seats[a].position, seats[b].position) <= link)
                    {
                        seen[b] = true;
                        pending.Enqueue(b);
                    }
                }
            }
            if (group.Count > best.Count) best = group;
        }
        return best;
    }

    private static Vector3 DeskCentreWorld(Transform room, List<Transform> seats)
    {
        if (seats.Count == 0) return room.position;
        Vector3 sum = Vector3.zero;
        foreach (Transform seat in seats) sum += seat.position;
        return sum / seats.Count;
    }

    private static MonoBehaviour BuildAnomoly13(GameObject floor)
    {
        Anomoly13 anomoly = Host<Anomoly13>(floor, "Anomoly#13");
        anomoly.classroomLights = new List<Light>();

        // Only two classrooms ship with a lamp. Clone one into the rooms that have
        // none, disabled, so there is something dark left to switch on.
        foreach (string roomName in Classrooms)
        {
            Transform room = floor.transform.Find(roomName);
            if (room == null) continue;

            // A lamp the map already had is left exactly as the map author set it -
            // ClassRoom Detail (3) is lit on purpose, and Anomoly20 hangs a body in it.
            Transform lamp = room.Find("ClassLight");
            if (lamp == null)
            {
                Transform template = FindLampTemplate(floor, room);
                if (template == null)
                {
                    Debug.LogWarning("Anomoly 13: no ClassLight to clone from.");
                    continue;
                }

                lamp = room.Find(ClonedLampName);
                if (lamp == null)
                {
                    GameObject clone = Object.Instantiate(template.gameObject, room);
                    clone.name = ClonedLampName;
                    clone.transform.localRotation = template.localRotation;
                    clone.transform.localScale = template.localScale;
                    lamp = clone.transform;
                }

                // Hang it over the middle of this room's own desks, at the template's
                // height. Copying the template's position instead put the lamp through
                // the wall: the two classroom blocks are mirrored, so the same local
                // coordinates land inside one room and eight metres outside the other.
                lamp.localPosition = DeskCentre(room, template.localPosition.y);

                // Every run, not just the run that created it. Instantiate copies the
                // template's active state and the map's two lamps disagree about it, so
                // without this the rooms cloned from the inactive one stay inactive and
                // switching the Light component on would never light them.
                lamp.gameObject.SetActive(true);
                Light cloned = lamp.GetComponent<Light>();
                if (cloned != null)
                {
                    cloned.enabled = false;
                    cloned.intensity = ClonedLampIntensity;
                    cloned.range = ClonedLampRange;
                }
            }

            Light light = lamp.GetComponent<Light>();
            if (light != null) anomoly.classroomLights.Add(light);
        }

        Debug.Log("Anomoly 13: " + anomoly.classroomLights.Count + " classroom lamps.");
        return anomoly;
    }

    private static MonoBehaviour BuildAnomoly18(GameObject floor)
    {
        Anomoly18 anomoly = Host<Anomoly18>(floor, "Anomoly#18");
        anomoly.props = new List<Transform>();

        foreach (string propName in GrowingProps)
        {
            Transform prop = floor.transform.Find(propName);
            if (prop != null) anomoly.props.Add(prop);
            else Debug.LogWarning("Anomoly 18: prop " + propName + " not found.");
        }

        // One room sign as well - a door plate that has quietly grown is nastier
        // than a bin, because you read it every round.
        Transform sign = FindDeep(floor.transform, "RoomSign");
        if (sign != null) anomoly.props.Add(sign);

        Debug.Log("Anomoly 18: " + anomoly.props.Count + " props.");
        return anomoly;
    }

    private static MonoBehaviour BuildAnomoly22(GameObject floor)
    {
        Anomoly22 anomoly = Host<Anomoly22>(floor, "Anomoly#22");
        anomoly.normalMaterial = LoadMaterial("M_PosterHCMUS");
        anomoly.anomolyMaterial = LoadMaterial("M_PosterUIT");
        anomoly.boardRenderers = new List<MeshRenderer>();

        Vector3 anchor, normal, up, across;
        if (!BoardBasis(floor, out anchor, out normal, out up, out across)) return anomoly;

        // Three sheets, so the call for papers is one notice among several instead of
        // the only thing on the board. A board holding a single sheet tells the player
        // exactly what to inspect; a full board makes them choose.
        MeshRenderer sheet = Pin(floor, "BoardPoster", anchor, normal, up,
                                 Vector3.zero, PosterSize, anomoly.normalMaterial);
        if (sheet != null) anomoly.boardRenderers.Add(sheet);
        PinVisionSheet(floor);
        Pin(floor, "BoardPosterSeminar", anchor, normal, up,
            across * PosterStep, PosterSize, LoadMaterial("M_PosterSeminar"));

        Debug.Log("Anomoly 22: three sheets on " + CorridorBoardName + ", centre at "
                  + anchor + ", facing " + normal + ".");
        return anomoly;
    }

    // The board's own frame: where a sheet hangs, which way it faces, which way is up
    // and which way runs along it.
    //
    // Only the axes are derived - the offsets are written down as constants. Deriving
    // the depth as well produced a different answer every time the bounds calculation
    // changed, and the window that actually reads is narrow: 9cm too far and the sheet
    // vanishes behind the wall. Keeping the axes derived still means the sheets follow
    // the board if it is ever moved or turned.
    private static bool BoardBasis(GameObject floor, out Vector3 anchor, out Vector3 normal,
                                   out Vector3 up, out Vector3 across)
    {
        anchor = Vector3.zero;
        normal = Vector3.forward;
        up = Vector3.up;
        across = Vector3.right;

        Transform room = floor.transform.Find(CorridorBoardRoom);
        Transform board = room != null ? room.Find(CorridorBoardName) : null;
        if (board == null)
        {
            Debug.LogWarning("Corridor board " + CorridorBoardRoom + "/" + CorridorBoardName + " not found.");
            return false;
        }

        // Measure the board's first renderer - its flat panel - and nothing else.
        // Encapsulating every renderer pulls in the frame behind the panel, which
        // pushes the computed face about 9cm proud of the surface you actually pin to
        // and leaves the sheets floating inside the frame's depth.
        Renderer panel = board.GetComponentInChildren<Renderer>();
        if (panel == null)
        {
            Debug.LogWarning("The corridor board has no renderer to measure.");
            return false;
        }

        // A board is a flat panel, so its thinnest bounds axis is the face normal, and
        // the other two give the surface to pin the sheets to.
        Bounds bounds = panel.bounds;
        int thin = 0;
        if (bounds.size.y < bounds.size[thin]) thin = 1;
        if (bounds.size.z < bounds.size[thin]) thin = 2;

        // Of the two remaining axes, one runs up the board and one runs across it.
        int upAxis = thin == 1 ? 2 : 1;
        int acrossAxis = thin == 0 ? 2 : 0;

        normal = Vector3.zero;
        normal[thin] = PosterFaceSign;
        up = Vector3.zero;
        up[upAxis] = 1f;
        across = Vector3.zero;
        across[acrossAxis] = 1f;

        // Anchored to the board's transform, not its bounds.
        anchor = board.position + up * PosterRise + normal * PosterDepth;
        return true;
    }

    // The left-hand sheet, the one drawn with the eyes. Shared, because anomoly 22
    // hangs it and anomoly 36 needs its transform to know where those eyes are - and
    // neither should have to run before the other. Pinning is derived and idempotent,
    // so hanging it twice puts it in exactly the same place.
    private static Transform PinVisionSheet(GameObject floor)
    {
        Vector3 anchor, normal, up, across;
        if (!BoardBasis(floor, out anchor, out normal, out up, out across)) return null;

        MeshRenderer sheet = Pin(floor, "BoardPosterVision", anchor, normal, up,
                                 across * -PosterStep, PosterSize, LoadMaterial("M_PosterVision"));
        return sheet != null ? sheet.transform : null;
    }

    // Places one sheet flat on the board face, offset sideways along it. Always
    // recomputed - these are derived from the board, so if the board moves the sheets
    // follow rather than staying where they were last put.
    private static MeshRenderer Pin(GameObject floor, string quadName, Vector3 anchor,
                                    Vector3 normal, Vector3 up, Vector3 offset,
                                    Vector2 size, Material material)
    {
        GameObject quad = FindOrCreateQuad(floor, quadName);
        quad.transform.position = anchor + offset;
        // A quad's visible face is its local +Z, so forward has to be the outward
        // normal. Facing it the other way culls the sheet from the side you read it.
        quad.transform.rotation = Quaternion.LookRotation(normal, up);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return renderer;
    }

    private static MonoBehaviour BuildAnomoly24(GameObject floor)
    {
        Anomoly24 anomoly = Host<Anomoly24>(floor, "Anomoly#24");
        anomoly.normalMaterial = LoadMaterial("M_LogoHCMUS");
        anomoly.anomolyMaterial = LoadMaterial("M_LogoUEH");

        MeshRenderer logo = Quad(floor, "SchoolLogo", LogoPosition, LogoEuler, LogoScale,
                                 anomoly.normalMaterial);
        anomoly.logoRenderers = new List<MeshRenderer> { logo };
        return anomoly;
    }

    private static MonoBehaviour BuildAnomoly26(GameObject floor)
    {
        Anomoly26 anomoly = Host<Anomoly26>(floor, "Anomoly#26");
        anomoly.fixtures = new List<Transform>();

        foreach (string lightName in CrookedCandidates)
        {
            Transform light = floor.transform.Find(lightName);
            if (light == null) continue;
            if (light.childCount == 0)
            {
                Debug.LogWarning("Anomoly 26: " + lightName + " has no fixture mesh to turn.");
                continue;
            }

            // Each ceiling light is one GameObject holding the Light component, with a
            // single child holding the fixture meshes. That child is taken by position
            // rather than by name: Unity numbered the copies Led, Led (1), Led (2)...,
            // so matching on "Led" would only ever find the first one.
            Transform fixtureMesh = light.GetChild(0);

            // Turning the mesh must not move the lamp itself, or the corridor lighting
            // shifts and gives the anomoly away.
            if (fixtureMesh.GetComponent<Light>() != null)
            {
                Debug.LogWarning("Anomoly 26: skipping " + lightName + " - its child carries the Light.");
                continue;
            }
            anomoly.fixtures.Add(fixtureMesh);
        }

        Debug.Log("Anomoly 26: " + anomoly.fixtures.Count + " fixtures.");
        return anomoly;
    }

    private static MonoBehaviour BuildAnomoly36(GameObject floor)
    {
        Anomoly36 anomoly = Host<Anomoly36>(floor, "Anomoly#36");
        anomoly.eyes = new List<Transform>();
        anomoly.travelX = EyeTravelX;
        anomoly.travelY = EyeTravelY;

        Transform sheet = PinVisionSheet(floor);
        if (sheet == null)
        {
            Debug.LogWarning("Anomoly 36: no vision sheet to put eyes on.");
            return anomoly;
        }

        // Placed off the sheet's own axes rather than the board's, so left stays left:
        // a quad's +x is the artwork's +u whichever way the board ends up facing, while
        // the board's "across" is just whichever world axis happened to be widest.
        Material iris = LoadMaterial("M_IrisVision");
        string[] names = { "BoardEyeLeft", "BoardEyeRight" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject quad = FindOrCreateQuad(floor, names[i]);

            // Always recomputed, like the sheets - the eyes are drawn on the paper, so
            // they have to move with it rather than staying where they were last put.
            quad.transform.rotation = sheet.rotation;
            quad.transform.position = sheet.position
                                    + sheet.right * (i == 0 ? -EyeAcross : EyeAcross)
                                    + sheet.up * EyeRise
                                    + sheet.forward * EyeDepthOffset;
            quad.transform.localScale = new Vector3(EyeSize, EyeSize, 1f);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            if (renderer != null && iris != null) renderer.sharedMaterial = iris;
            anomoly.eyes.Add(quad.transform);
        }

        Debug.Log("Anomoly 36: two eyes on the vision sheet at " + sheet.position + ".");
        return anomoly;
    }


    // ---- helpers -------------------------------------------------------------

    // Finds or creates the Anomoly#N child and its component, matching how the
    // existing anomoly objects sit directly under the Floor root.
    private static T Host<T>(GameObject floor, string objectName) where T : MonoBehaviour
    {
        Transform host = floor.transform.Find(objectName);
        if (host == null)
        {
            GameObject created = new GameObject(objectName);
            created.transform.SetParent(floor.transform, false);
            host = created.transform;
        }

        T component = host.GetComponent<T>();
        if (component == null) component = host.gameObject.AddComponent<T>();
        return component;
    }

    private static void Register(AnomolyManager manager, MonoBehaviour anomoly)
    {
        if (anomoly == null) return;
        if (!manager.anomolies.Contains(anomoly)) manager.anomolies.Add(anomoly);
    }

    // A bare quad under the Floor root, with its collider stripped - a sign should
    // never block the player or swallow an interaction ray.
    private static GameObject FindOrCreateQuad(GameObject floor, string quadName)
    {
        Transform existing = floor.transform.Find(quadName);
        if (existing != null) return existing.gameObject;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = quadName;
        quad.transform.SetParent(floor.transform, false);

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        return quad;
    }

    private static MeshRenderer Quad(GameObject floor, string quadName, Vector3 position,
                                     Vector3 euler, Vector3 scale, Material material)
    {
        bool existed = floor.transform.Find(quadName) != null;
        GameObject quad = FindOrCreateQuad(floor, quadName);
        if (!existed)
        {
            // Only place it the first time - after that, keep wherever it was nudged to.
            quad.transform.localPosition = position;
            quad.transform.localRotation = Quaternion.Euler(euler);
            quad.transform.localScale = scale;
        }

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return renderer;
    }

    private static Material LoadMaterial(string materialName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ArtPath + materialName + ".mat");
        if (material == null) Debug.LogWarning("Anomoly setup: material " + materialName + " not found in " + ArtPath);
        return material;
    }

    // The centre of a room's desks, in the room's own local space, at a given height.
    // Rooms differ in size and the two blocks are mirrored, so this is the only
    // placement that lands inside every one of them.
    private static Vector3 DeskCentre(Transform room, float height)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Transform child in room)
        {
            if (!child.name.StartsWith("chair1")) continue;
            sum += child.localPosition;
            count++;
        }
        if (count == 0) return new Vector3(0f, height, 0f);

        Vector3 centre = sum / count;
        centre.y = height;
        return centre;
    }

    // The two rooms that ship with a lamp hang it at different local positions, and
    // the classrooms come in two mirrored blocks. Clone from the lamp on the same
    // side of the corridor, or the copy ends up inside a wall.
    private static Transform FindLampTemplate(GameObject floor, Transform target)
    {
        Transform best = null;
        float bestDistance = float.MaxValue;

        foreach (string roomName in Classrooms)
        {
            Transform room = floor.transform.Find(roomName);
            if (room == null || room == target) continue;
            Transform lamp = room.Find("ClassLight");
            if (lamp == null || lamp.GetComponent<Light>() == null) continue;

            float distance = Mathf.Abs(room.localPosition.x - target.localPosition.x);
            if (distance < bestDistance) { bestDistance = distance; best = lamp; }
        }
        return best;
    }

    private static Transform FindDeep(Transform root, string targetName)
    {
        foreach (Transform child in root)
        {
            if (child.name == targetName) return child;
            Transform found = FindDeep(child, targetName);
            if (found != null) return found;
        }
        return null;
    }
}
