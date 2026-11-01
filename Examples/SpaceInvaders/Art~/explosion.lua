-- Draws the explosion animation: eight 32x32 frames of a pixel-art burst, white with a light grey rim, so the game
-- can tint it per invader. Run headless:
--   Aseprite.exe -b --script explosion.lua --script-param out=<frames folder> --script-param source=<explosion.aseprite>
local out = app.params["out"]
local source = app.params["source"]

local FRAMES = 8
local SIZE = 32
local CENTRE = (SIZE - 1) / 2

local function rgba(r, g, b, a)
  return app.pixelColor.rgba(r, g, b, a)
end

local function clamp(v, lo, hi)
  return math.max(lo, math.min(hi, v))
end

local function ease_out(t)
  return 1 - (1 - t) * (1 - t)
end

-- A small deterministic hash, so the art is the same every time the script runs.
local function hash(n)
  local x = math.sin(n * 12.9898) * 43758.5453
  return x - math.floor(x)
end

local function plot(img, x, y, color)
  if x >= 0 and y >= 0 and x < SIZE and y < SIZE then
    img:drawPixel(x, y, color)
  end
end

local function draw_frame(img, f)
  local t = (f - 1) / (FRAMES - 1)

  -- The hot core: a bright disc that swells and is gone by 60 % of the animation.
  if t < 0.6 then
    local radius = 3 + 5 * (t / 0.6)
    local alpha = math.floor(255 * (1 - (t / 0.6) ^ 2))
    for y = 0, SIZE - 1 do
      for x = 0, SIZE - 1 do
        local d = math.sqrt((x - CENTRE) ^ 2 + (y - CENTRE) ^ 2)
        if d <= radius then
          local v = d < radius * 0.55 and 255 or 232
          plot(img, x, y, rgba(v, v, v, alpha))
        end
      end
    end
  end

  -- The shock ring: grows, thins and crumbles with a checker dither.
  local ring_radius = 3 + 12 * ease_out(t)
  local thickness = math.max(1, math.floor(3 * (1 - t) + 0.5))
  local ring_alpha = math.floor(255 * (1 - t * t))
  for y = 0, SIZE - 1 do
    for x = 0, SIZE - 1 do
      local dx, dy = x - CENTRE, y - CENTRE
      local angle = math.atan(dy, dx)
      local wobble = math.sin(angle * 5 + f) * 0.9 * t
      local d = math.sqrt(dx * dx + dy * dy)
      if math.abs(d - (ring_radius + wobble)) <= thickness / 2 + 0.25 then
        local crumble = t > 0.45 and (x + y) % 2 == 0
        if not crumble then
          plot(img, x, y, rgba(214, 214, 214, ring_alpha))
        end
      end
    end
  end

  -- Sparks flying outwards, big at first and single pixels at the end.
  local sparks = 10
  for k = 0, sparks - 1 do
    local angle = k * (2 * math.pi / sparks) + 0.3
    local r = ring_radius * (0.85 + 0.45 * hash(k + 1))
    local sx = math.floor(CENTRE + math.cos(angle) * r + 0.5)
    local sy = math.floor(CENTRE + math.sin(angle) * r + 0.5)
    local alpha = math.floor(255 * (1 - t))
    local color = rgba(255, 255, 255, alpha)
    plot(img, sx, sy, color)
    if t < 0.5 then
      plot(img, sx + 1, sy, color)
      plot(img, sx, sy + 1, color)
      plot(img, sx + 1, sy + 1, color)
    end
  end
end

local sprite = Sprite(SIZE, SIZE, ColorMode.RGB)
for _ = 2, FRAMES do
  sprite:newEmptyFrame()
end
sprite.layers[1].name = "burst"

for f = 1, FRAMES do
  local image = Image(SIZE, SIZE, ColorMode.RGB)
  draw_frame(image, f)
  sprite:newCel(sprite.layers[1], f, image, Point(0, 0))
  image:saveAs(out .. "/explosion_" .. (f - 1) .. ".png")
end

for _, frame in ipairs(sprite.frames) do
  frame.duration = 0.05
end
sprite:newTag(1, FRAMES).name = "burst"
sprite:saveAs(source)
