﻿using UnityEngine;
using System.Collections;

namespace Leap.Unity.Interaction {

  public abstract class IControllerBase : ScriptableObject {
    protected InteractionBehaviour _obj;

    public virtual void Init(InteractionBehaviour obj) {
      _obj = obj;
    }
  }
}
