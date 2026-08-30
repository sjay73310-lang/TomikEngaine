# Tomk Script Language Draft

Tomk Script is planned as a personal C#-like language for Tomk Engine.

## Style

- Classes attach behavior to entities.
- Types are explicit: `float`, `int`, `bool`, `string`, `Vector2`, `Vector3`.
- Engine callbacks use `fn start()`, `fn update(delta: float)`, and `fn fixedUpdate(delta: float)`.
- Scripts can access `entity`, `Input`, `Physics`, `Animator`, and `Scene`.

## Example

```tomk
class GunController : Component {
    fireRate: float = 0.12;
    timer: float = 0.0;

    fn update(delta: float) {
        timer = timer - delta;

        if Input.mouseDown(0) and timer <= 0.0 {
            fire();
            timer = fireRate;
        }
    }

    fn fire() {
        Physics.raycast(Camera.forward(), 100.0);
    }
}
```
