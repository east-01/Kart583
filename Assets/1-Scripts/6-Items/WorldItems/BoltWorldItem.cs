using UnityEngine;
using System;
using System.Collections.Generic;

public class BoltWorldItem : WorldItem
{

    public float speed = 15f;
    private float usedSpeed; // The current speed we're using. This is so we can have the initial velocity match kart speed at the start
    public Vector3 dir;
    public List<ParticleSystem> systems;
    public float collCooldown;

    private void FixedUpdate() {
        if(collCooldown > 0) {
            collCooldown -= Time.fixedDeltaTime;
            if(collCooldown < 0) collCooldown = 0;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.velocity = dir*usedSpeed + Vector3.up*Math.Min(rb.velocity.y, 0);
        Debug.DrawRay(transform.position, dir, Color.blue, Time.deltaTime);
    }  

    new public void OnCollisionEnter(Collision collision)
    {
        // !!WARNING!! this might be essential for overriding the OnCollisionEnter in WorldItem, since calls in there are still important
        // This is a stupid fix and i need to do more research but i dont have time.
        base.OnCollisionEnter(collision);

        // Check if the collision involves a surface with a collider
        if(collision.collider == null) return;
        if(collCooldown > 0.001f) return;

        usedSpeed = speed;

        Rigidbody rb = GetComponent<Rigidbody>();
        
        // Get the collision normal (direction perpendicular to the collision surface)
        Vector3 collisionNormal = collision.GetContact(0).normal;
        if(Vector3.Dot(collisionNormal, Vector3.up) > 0.5) return;

        // Calculate the new velocity by reflecting the current velocity against the collision normal
        dir = Vector3.Reflect(dir, collisionNormal);

        rb.velocity = dir*usedSpeed;
        rb.MovePosition(transform.position+dir);

        collCooldown = 0.5f;
    }

    protected override void Internal_ActivateItem(ItemSpawnData spawnData)
    {
        lifeTime = 12f; // 12s of lifetime

        KartController kc = OwnerKartManager.GetKartController();

        //Needs to change
        float forwardBackward = Mathf.Sign(spawnData.stickDirection.y);
        if(forwardBackward == 0) forwardBackward = 1;
        transform.position = OwnerKartManager.gameObject.transform.position + forwardBackward*3f*kc.KartForward.normalized + kc.up*2;
        dir = kc.KartForward;
        GetComponent<Rigidbody>().velocity = kc.TrackVelocity;

        usedSpeed = Math.Max(kc.CurrentMaxSpeed*1.5f, speed);

        // TODO: Play activation animation and sound
        systems.ForEach(pe => pe.Play());

    }

    protected override void Internal_ItemDestroyed()
    {
        systems.ForEach(pe => pe.Stop());
    }

    protected override void Internal_ItemHit(string hitPlayerUUID)
    {
        KartManager hitKM = gameplayManager.PlayerManager.SearchForKartManager(hitPlayerUUID);
        if(hitKM == null) {
            Debug.LogError($"Internal_ItemHit could not locate KartManager from uuid \"{hitPlayerUUID}\"");
            return;
        }
        
        hitKM.GetKartController().damageCooldown = 3.5f;
        Internal_ItemDestroyed();
    }
    
}