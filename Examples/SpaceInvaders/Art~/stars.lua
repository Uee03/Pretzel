-- Draws three seamless 256x256 star tiles for the parallax backdrop: far (tiny dim dots), mid (2x2 stars) and near
-- (a few plus-shaped sparkles). Every star sits inside the tile with a margin, so a tile repeats with no seam.
--   Aseprite.exe -b --script stars.lua --script-param out=<folder> --script-param source=<folder for .aseprite>
local out = app.params["out"]
local source = app.params["source"]

local SIZE = 256

local function rgba(r, g, b, a)
  return app.pixelColor.rgba(r, g, b, a)
end

-- A deterministic linear congruential generator.
local function generator(seed)
  local state = seed
  return function()
    state = (state * 1664525 + 1013904223) % 4294967296
    return state / 4294967296
  end
end

local function make_tile(name, draw)
  local sprite = Sprite(SIZE, SIZE, ColorMode.RGB)
  local image = Image(SIZE, SIZE, ColorMode.RGB)
  draw(image)
  sprite:newCel(sprite.layers[1], 1, image, Point(0, 0))
  image:saveAs(out .. "/" .. name .. ".png")
  sprite:saveAs(source .. "/" .. name .. ".aseprite")
  sprite:close()
end

local function place(random, margin)
  return margin + math.floor(random() * (SIZE - 2 * margin)), margin + math.floor(random() * (SIZE - 2 * margin))
end

make_tile("stars_far", function(image)
  local random = generator(11)
  for _ = 1, 150 do
    local x, y = place(random, 2)
    local brightness = 120 + math.floor(random() * 80)
    image:drawPixel(x, y, rgba(brightness, brightness + 10, brightness + 40, 255))
  end
end)

make_tile("stars_mid", function(image)
  local random = generator(23)
  for _ = 1, 38 do
    local x, y = place(random, 3)
    local warm = random() < 0.2
    local color = warm and rgba(255, 226, 190, 255) or rgba(196, 214, 255, 255)
    for dy = 0, 1 do
      for dx = 0, 1 do
        image:drawPixel(x + dx, y + dy, color)
      end
    end
  end
end)

make_tile("stars_near", function(image)
  local random = generator(37)
  for _ = 1, 12 do
    local x, y = place(random, 6)
    local warm = random() < 0.3
    local core = warm and rgba(255, 238, 200, 255) or rgba(255, 255, 255, 255)
    local arm = warm and rgba(255, 196, 120, 200) or rgba(150, 190, 255, 200)
    local tip = warm and rgba(255, 196, 120, 110) or rgba(150, 190, 255, 110)
    image:drawPixel(x, y, core)
    for d = 1, 2 do
      local color = d == 1 and arm or tip
      image:drawPixel(x + d, y, color)
      image:drawPixel(x - d, y, color)
      image:drawPixel(x, y + d, color)
      image:drawPixel(x, y - d, color)
    end
  end
end)
