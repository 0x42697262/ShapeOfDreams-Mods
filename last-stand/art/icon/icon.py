import math, os, random
random.seed(7)
W=512; cx,cy=256,205
O="#120f26"
def P(pts): return " ".join(f"{x:.1f},{y:.1f}" for x,y in pts)
def pol(r,a,ox=cx,oy=cy): return (ox+r*math.cos(a), oy+r*math.sin(a))

s=[]
s.append(f'''<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{W}" viewBox="0 0 {W} {W}">
<defs>
 <radialGradient id="bg" cx="50%" cy="40%" r="75%">
  <stop offset="0" stop-color="#3a3f7a"/><stop offset="0.55" stop-color="#1c1d45"/><stop offset="1" stop-color="#08081a"/></radialGradient>
 <radialGradient id="glow" cx="50%" cy="50%" r="50%">
  <stop offset="0" stop-color="#e8fbff" stop-opacity="0.55"/><stop offset="0.35" stop-color="#7fe3ff" stop-opacity="0.3"/><stop offset="1" stop-color="#7fe3ff" stop-opacity="0"/></radialGradient>
 <radialGradient id="mon" cx="40%" cy="35%" r="70%">
  <stop offset="0" stop-color="#e2a3ff"/><stop offset="0.5" stop-color="#9b3df0"/><stop offset="1" stop-color="#4b1490"/></radialGradient>
 <radialGradient id="monr" cx="40%" cy="35%" r="70%">
  <stop offset="0" stop-color="#ffd0c0"/><stop offset="0.5" stop-color="#ff4a3d"/><stop offset="1" stop-color="#8a0f1f"/></radialGradient>
 <radialGradient id="monc" cx="40%" cy="35%" r="70%">
  <stop offset="0" stop-color="#c8fff4"/><stop offset="0.5" stop-color="#25d6c0"/><stop offset="1" stop-color="#0b6a73"/></radialGradient>
 <linearGradient id="gold" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#fff6c4"/><stop offset="0.45" stop-color="#ffd24a"/><stop offset="1" stop-color="#f08a00"/></linearGradient>
 <linearGradient id="blade" x1="0" y1="0" x2="1" y2="0">
  <stop offset="0" stop-color="#ffffff"/><stop offset="0.5" stop-color="#d9f4ff"/><stop offset="0.5" stop-color="#8fb8e6"/><stop offset="1" stop-color="#6a8fc8"/></linearGradient>
 <linearGradient id="shade" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0.45" stop-color="#05040f" stop-opacity="0"/><stop offset="1" stop-color="#05040f" stop-opacity="0.85"/></linearGradient>
</defs>
<rect width="{W}" height="{W}" fill="url(#bg)"/>''')

for r,op in [(230,0.10),(200,0.14),(160,0.10),(120,0.08)]:
    s.append(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="none" stroke="#9fb0ff" stroke-opacity="{op}" stroke-width="3"/>')

s.append(f'<circle cx="{cx}" cy="{cy}" r="178" fill="none" stroke="{O}" stroke-width="16"/>')
s.append(f'<circle cx="{cx}" cy="{cy}" r="178" fill="none" stroke="#c9d3ff" stroke-width="7"/>')
for k in range(36):
    a=k*math.pi/18
    s.append(f'<polygon points="{P([pol(182,a-0.035),pol(182,a+0.035),pol(198,a)])}" fill="#c9d3ff" stroke="{O}" stroke-width="3" stroke-linejoin="round"/>')

def star(rlong,rshort,rin,light,dark):
    out=[]
    for k in range(8):
        a=k*math.pi/4 - math.pi/2
        r=rlong if k%2==0 else rshort
        tip=pol(r,a); vl=pol(rin,a-math.pi/8); vr=pol(rin,a+math.pi/8)
        out.append(f'<polygon points="{P([(cx,cy),vl,tip])}" fill="{light}" stroke="{O}" stroke-width="5" stroke-linejoin="round"/>')
        out.append(f'<polygon points="{P([(cx,cy),tip,vr])}" fill="{dark}" stroke="{O}" stroke-width="5" stroke-linejoin="round"/>')
    return out
s+=star(150,105,48,"#eef2ff","#9aa6e8")

s.append(f'<circle cx="{cx}" cy="{cy}" r="120" fill="url(#glow)"/>')

def sword(x,y):
    g=[]
    g.append(f'<polygon points="{P([(x-15,y-10),(x+15,y-10),(x+15,y+80),(x,y+110),(x-15,y+80)])}" fill="url(#blade)" stroke="{O}" stroke-width="6" stroke-linejoin="round"/>')
    g.append(f'<rect x="{x-5}" y="{y-78}" width="10" height="70" rx="4" fill="#7a4a2a" stroke="{O}" stroke-width="5"/>')
    g.append(f'<rect x="{x-48}" y="{y-20}" width="96" height="18" rx="9" fill="url(#gold)" stroke="{O}" stroke-width="6"/>')
    g.append(f'<circle cx="{x}" cy="{y-82}" r="13" fill="url(#gold)" stroke="{O}" stroke-width="6"/>')
    g.append(f'<circle cx="{x}" cy="{y-11}" r="6" fill="#7fe3ff" stroke="{O}" stroke-width="3"/>')
    return g
s+=sword(cx,cy-20)

def monster(x,y,sc,grad,flip=False,eye="#fff6a0"):
    pts=[]
    n=14
    for i in range(n):
        a=i*2*math.pi/n
        r=(46 if i%2==0 else 34)+random.uniform(-4,4)
        pts.append((r*math.cos(a), r*math.sin(a)))
    f=-1 if flip else 1
    g=[f'<g transform="translate({x},{y}) scale({sc*f},{sc})">']
    g.append(f'<polygon points="{P(pts)}" fill="url(#{grad})" stroke="{O}" stroke-width="6" stroke-linejoin="round"/>')
    for ex in (-14,14):
        g.append(f'<ellipse cx="{ex}" cy="-2" rx="9" ry="11" fill="{eye}" stroke="{O}" stroke-width="3"/>')
        g.append(f'<circle cx="{ex+2}" cy="1" r="4" fill="{O}"/>')
    g.append(f'<polygon points="-26,-20 -4,-8 -6,-4 -27,-13" fill="{O}"/>')
    g.append(f'<polygon points="26,-20 4,-8 6,-4 27,-13" fill="{O}"/>')
    g.append(f'<path d="M -14 18 Q 0 10 14 18 L 9 22 L 5 17 L 0 22 L -5 17 L -9 22 Z" fill="{O}"/>')
    g.append('</g>')
    return g

s+=monster(78,88,1.05,"mon")
s+=monster(436,92,0.95,"monc",flip=True)
s+=monster(58,250,0.9,"monr")
s+=monster(456,258,1.0,"mon",flip=True)
s+=monster(150,38,0.6,"mon")
s+=monster(368,36,0.6,"monr",flip=True)

s.append(f'<rect width="{W}" height="{W}" fill="url(#shade)"/>')

def title(text,y,size,sp):
    t=f'font-family="Fira Sans" font-weight="900" font-size="{size}" letter-spacing="{sp}" text-anchor="middle"'
    return [
      f'<text x="{cx+5}" y="{y+8}" {t} fill="{O}" stroke="{O}" stroke-width="26" stroke-linejoin="round">{text}</text>',
      f'<text x="{cx}" y="{y}" {t} fill="url(#gold)" stroke="{O}" stroke-width="22" stroke-linejoin="round" paint-order="stroke">{text}</text>',
      f'<text x="{cx}" y="{y}" {t} fill="url(#gold)">{text}</text>',
    ]
s+=title("LAST",392,118,6)
s+=title("STAND",494,118,4)
s.append('</svg>')
open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'icon.svg'),'w').write("\n".join(s))
