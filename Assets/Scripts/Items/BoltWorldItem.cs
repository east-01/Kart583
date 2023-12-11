using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public class BoltWorldItem : WorldItem
{

    public List<ParticleSystem> systems;

    private void Update() {
        if(lifeTime <= 0) return;
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 targVel = transform.forward;
        rb.AddForce(rb.velocity-targVel, ForceMode.VelocityChange);
    }  

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        // Check if the collision involves a surface with a collider
        if (collision.collider != null)
        {
            // Get the collision normal (direction perpendicular to the collision surface)
            Vector3 collisionNormal = collision.contacts[0].normal;

            // Calculate the new velocity by reflecting the current velocity against the collision normal
            Vector3 newVelocity = Vector3.Reflect(rb.velocity, collisionNormal);

            // Apply the new velocity to the Rigidbody
            rb.velocity = newVelocity;
            transform.forward = newVelocity.normalized;
        }
    }

    public override void ActivateItem(GameObject owner, Vector2 directionInput)
    {

        Owner = owner;
        lifeTime = 30f; // 30s of lifetime

        KartController kc = owner.GetComponent<KartController>();

        //Needs to change
        float forwardBackward = Mathf.Sign(directionInput.y);
        if(forwardBackward == 0) forwardBackward = 1;
        transform.position = owner.transform.position + forwardBackward*3f*kc.KartForward.normalized + kc.up*2;
        transform.forward = kc.KartForward;
        GetComponent<Rigidbody>().velocity = kc.TrackVelocity;

        // TODO: Play activation animation and sound
        systems.ForEach(pe => pe.Play());

    }

    public override void ItemDestroyed()
    {
        print("TODO: Item bolt destroyed");
        systems.ForEach(pe => pe.Stop());
        Destroy(gameObject);
    }

    public override void ItemHit(GameObject hitPlayer)
    {
        print("TODO: Player hit by bolt");
        ItemDestroyed();
    }
    
}