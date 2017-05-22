/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Interaction.Internal {

  /// <summary>
  /// Contact Bones store data for the colliders and rigidbodies in each
  /// bone of the contact-related representation of an InteractionHand.
  /// They also notify the InteractionHand of collisions for further
  /// processing.
  /// 
  /// To correctly initialize a newly-constructed ContactBone, you must
  /// set its interactionController, body, and collider.
  /// </summary>
  [AddComponentMenu("")]
  public class ContactBone : MonoBehaviour {

    /// <summary>
    /// ContactBones minimally require references to their InteractionControllerBase,
    /// their Rigidbody, and strictly one (1) collider.
    /// </summary>
    public InteractionController interactionController;

    /// <summary>
    /// The Rigidbody of this ContactBone. This field must not be null for the ContactBone
    /// to work correctly.
    /// </summary>
    public Rigidbody body;

    /// <summary>
    /// The Collider of this ContactBone. This field must not be null for the ContactBone
    /// to work correctly.
    /// </summary>
    #if UNITY_EDITOR
    new
    #endif
    public Collider collider;

    /// <summary>
    /// Soft contact logic requires knowing the "width" of a ContactBone along its axis.
    /// </summary>
    public float width {
      get {
        Vector3 scale = collider.transform.lossyScale;
        if (collider is SphereCollider) {
          SphereCollider sphere = collider as SphereCollider;
          return Mathf.Min(sphere.radius * scale.x,
                 Mathf.Min(sphere.radius * scale.y,
                           sphere.radius * scale.z)) * 2F;
        }
        else if (collider is CapsuleCollider) {
          CapsuleCollider capsule = collider as CapsuleCollider;
          return Mathf.Min(capsule.radius * scale.x,
                 Mathf.Min(capsule.radius * scale.y,
                           capsule.radius * scale.z)) * 2F;
        }
        else if (collider is BoxCollider) {
          BoxCollider box = collider as BoxCollider;
          return Mathf.Min(box.size.x * scale.x,
                 Mathf.Min(box.size.y * scale.y,
                           box.size.z * scale.z));
        }
        else {
          return Mathf.Min(collider.bounds.size.x * scale.x,
                 Mathf.Min(collider.bounds.size.y * scale.y,
                           collider.bounds.size.z * scale.z));
        }
      }
    }

    /// <summary>
    /// InteractionHands use ContactBones to store additional, hand-specific data.
    /// Other InteractionControllerBase implementors need not set this field.
    /// </summary>
    public InteractionHand interactionHand;

    /// <summary>
    /// InteractionHands use ContactBones to store additional, hand-specific data.
    /// Other InteractionControllerBase implementors need not set this field.
    /// </summary>
    public FixedJoint joint;

    /// <summary>
    /// InteractionHands use ContactBones to store additional, hand-specific data.
    /// Other InteractionControllerBase implementors need not set this field.
    /// </summary>
    public FixedJoint metacarpalJoint;

    /// <summary>
    /// ContactBones remember their last target position; interaction controllers
    /// use this to know when to switch to soft contact mode.
    /// </summary>
    public Vector3 lastTargetPosition;

    public float _lastObjectTouchedAdjustedMass;

    void Start() {
      interactionController.manager.contactBoneBodies[body] = this;
    }

    void OnDestroy() {
      interactionController.manager.contactBoneBodies.Remove(body);
    }

    void OnCollisionEnter(Collision collision) {
      IInteractionBehaviour interactionObj;
      if (collision.rigidbody == null) {
        Debug.LogError("Contact Bone collided with non-rigidbody collider: " + collision.collider.name + ". "
                     + "Please enable automatic layer generation in the Interaction Manager, "
                     + "or ensure the Interaction layer only contains Interaction Behaviours.");
      }

      if (interactionController.manager.interactionObjectBodies.TryGetValue(collision.rigidbody, out interactionObj)) {
        _lastObjectTouchedAdjustedMass = collision.rigidbody.mass;
        if (interactionObj is InteractionBehaviour) {
          switch ((interactionObj as InteractionBehaviour).contactForceMode) {
            case InteractionBehaviour.ContactForceMode.UI:
              _lastObjectTouchedAdjustedMass *= 10F;
              break;
            case InteractionBehaviour.ContactForceMode.Object: default:
              if (interactionHand != null) {
                _lastObjectTouchedAdjustedMass *= 0.1F;
              }
              else {
                _lastObjectTouchedAdjustedMass *= 3F;
              }
              break;
          }
        }

        interactionController.NotifyContactBoneCollisionEnter(this, interactionObj, false);
      }
    }

    void OnCollisionExit(Collision collision) {
      IInteractionBehaviour interactionObj;
      if (interactionController.manager.interactionObjectBodies.TryGetValue(collision.rigidbody, out interactionObj)) {
        interactionController.NotifyContactBoneCollisionExit(this, interactionObj, false);
      }
    }

    void OnTriggerEnter(Collider collider) {
      if (collider.attachedRigidbody == null) {
        Debug.LogError("Contact Bone collided with non-rigidbody collider: " + collider.name + ". "
                     + "Please enable automatic layer generation in the Interaction Manager, "
                     + "or ensure the Interaction layer only contains Interaction Behaviours.");
      }
      IInteractionBehaviour interactionObj;
      if (interactionController.manager.interactionObjectBodies.TryGetValue(collider.attachedRigidbody, out interactionObj)) {
        interactionController.NotifyContactBoneCollisionEnter(this, interactionObj, true);

        interactionController.NotifySoftContactCollisionEnter(this, interactionObj, collider);
      }
    }

    void OnTriggerExit(Collider collider) {
      IInteractionBehaviour interactionObj;
      if (interactionController.manager.interactionObjectBodies.TryGetValue(collider.attachedRigidbody, out interactionObj)) {
        interactionController.NotifyContactBoneCollisionExit(this, interactionObj, true);

        interactionController.NotifySoftContactCollisionExit(this, interactionObj, collider);
      }
    }

  }

}
