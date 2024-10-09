#include <Jolt/Jolt.h>   // Jolt Physics main header
#include <iostream>
#include <vector>

// Assumed headers for Jolt Physics setup
#include <Jolt/Physics/Body/Body.h>
#include <Jolt/Physics/PhysicsSystem.h>
#include <Jolt/Physics/Collision/Shape/BoxShape.h>
#include <Jolt/Physics/Collision/Shape/CompoundShape.h>

// starting with `physicsSystem`

// Dozer stats
const float DOZER_LENGTH = 112.75f;   // inches
const float DOZER_WIDTH = 27.75f;     // inches
const float FRAME_HEIGHT_MIN = 22.31f; // inches
const float FRAME_HEIGHT_MAX = 29.31f; // inches
const float BUCKET_MAX_HEIGHT = 62.5f; // inches
const float BUCKET_CLEARANCE_MAX = 37.5f; // inches
const float TURNING_DIAMETER = 163.0f; // inches
const float DOZER_WEIGHT = 2400.0f;    // lbs, convert to kg (approx. 1088.6 kg)

// Utility function to convert inches to meters (Jolt uses SI units)
inline float inchesToMeters(float inches) {
    return inches * 0.0254f;
}

int main() {
    // Convert dozer dimensions from inches to meters for Jolt Physics
    float dozer_length_m = inchesToMeters(DOZER_LENGTH);
    float dozer_width_m = inchesToMeters(DOZER_WIDTH);
    float frame_height_avg_m = inchesToMeters((FRAME_HEIGHT_MIN + FRAME_HEIGHT_MAX) / 2.0f);
    float dozer_weight_kg = DOZER_WEIGHT * 0.453592f;  // Convert lbs to kg

    // Step 1: Create the dozer chassis (as a box collider)
    Body* dozerChassis = new RigidBody(BodyCreationSettings(
        new BoxShape(Vec3(dozer_length_m / 2, frame_height_avg_m / 2, dozer_width_m / 2)),  // Half extents
        Vec3(0, frame_height_avg_m / 2, 0),  // Initial position
        Quat::sIdentity(),  // No rotation initially
        EMotionType::Dynamic,  // Dynamic body for simulation
        PhysicsLayer::MOVABLE));

    dozerChassis->SetMass(dozer_weight_kg);  // Set the mass

    // Adjust the center of mass (slightly below the center for stability)
    Vec3 inertia = dozerChassis->GetShape()->GetMassProperties()->mInertia;
    dozerChassis->SetInertia(Vec3(inertia.x, inertia.y * 0.8f, inertia.z));  // Reduce Y-axis inertia for stability

    // Step 2: Define the bucket shape and constraints
    // simple bucket
    float bucket_max_height_m = inchesToMeters(BUCKET_MAX_HEIGHT);
    float bucket_clearance_max_m = inchesToMeters(BUCKET_CLEARANCE_MAX);

    Body* bucket = new RigidBody(BodyCreationSettings(
        new BoxShape(Vec3(dozer_width_m / 2, bucket_max_height_m / 2, dozer_length_m / 4)),  // Example bucket shape
        Vec3(0, bucket_clearance_max_m / 2, 0),  // Position relative to the chassis
        Quat::sIdentity(),
        EMotionType::Dynamic,
        PhysicsLayer::MOVABLE));

    bucket->SetMass(dozer_weight_kg * 0.1f);  // Assume the bucket weighs 10% of the chassis weight

    // Step 3: Attach the bucket to the chassis with a joint
    // hinge joint or slider joint??

    // Step 4: Add the bodies to the physics system
    physicsSystem->AddBody(dozerChassis);
    physicsSystem->AddBody(bucket);

    // Step 5: Simulate the movement of the dozer
    // Apply forces or set velocities for movement (tank-like controls)
    // For simple movement simulation:
    Vec3 forward_force = Vec3(100.0f, 0, 0);  // Apply a forward force to move the dozer
    dozerChassis->AddForce(forward_force);

    // To turn, apply opposite forces to the left and right treads
    float turn_force_left = 50.0f;   // Force for the left treads
    float turn_force_right = -50.0f; // Opposing force for the right treads

    dozerChassis->AddForceAtPoint(Vec3(turn_force_left, 0, 0), Vec3(-dozer_width_m / 2, 0, 0));   // Left side
    dozerChassis->AddForceAtPoint(Vec3(turn_force_right, 0, 0), Vec3(dozer_width_m / 2, 0, 0));  // Right side

    // sim loop TO-DO
    
    return 0;
}
