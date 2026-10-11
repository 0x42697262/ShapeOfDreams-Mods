import base64, os
HERE=os.path.dirname(os.path.abspath(__file__))
W,H=1280,720
O="#120f26"
def uri(p): return "data:image/png;base64,"+base64.b64encode(open(os.path.join(HERE,p),'rb').read()).decode()
s=[f'''<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{W}" height="{H}" viewBox="0 0 {W} {H}">
<defs>
 <linearGradient id="gold" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#fff6c4"/><stop offset="0.45" stop-color="#ffd24a"/><stop offset="1" stop-color="#f08a00"/></linearGradient>
 <linearGradient id="panel" x1="0" y1="0" x2="1" y2="0">
  <stop offset="0" stop-color="#070a1c" stop-opacity="0.85"/><stop offset="0.45" stop-color="#070a1c" stop-opacity="0.55"/><stop offset="1" stop-color="#070a1c" stop-opacity="0.35"/></linearGradient>
 <linearGradient id="band" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#070a1c" stop-opacity="0"/><stop offset="0.45" stop-color="#070a1c" stop-opacity="0.85"/><stop offset="1" stop-color="#070a1c" stop-opacity="0.95"/></linearGradient>
 <filter id="soft" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="8"/></filter>
</defs>
<image x="0" y="0" width="{W}" height="{H}" xlink:href="{uri('bgshot.png')}"/>
<rect width="{W}" height="{H}" fill="url(#panel)"/>''']

def title(text,x,y,size,sp,anchor="start",fill="url(#gold)",sw=24):
    t=f'font-family="Fira Sans" font-weight="900" font-size="{size}" letter-spacing="{sp}" text-anchor="{anchor}"'
    return [
      f'<text x="{x+6}" y="{y+9}" {t} fill="{O}" stroke="{O}" stroke-width="{sw+4}" stroke-linejoin="round">{text}</text>',
      f'<text x="{x}" y="{y}" {t} fill="{fill}" stroke="{O}" stroke-width="{sw}" stroke-linejoin="round" paint-order="stroke">{text}</text>',
      f'<text x="{x}" y="{y}" {t} fill="{fill}">{text}</text>',
    ]

def tile(n,img,x,y,w,h,caption,badge=None):
    cid=f"c{n}"
    g=[f'<clipPath id="{cid}"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="14"/></clipPath>']
    g.append(f'<rect x="{x-2}" y="{y+8}" width="{w+4}" height="{h+4}" rx="18" fill="#000" fill-opacity="0.6" filter="url(#soft)"/>')
    g.append(f'<rect x="{x-6}" y="{y-6}" width="{w+12}" height="{h+12}" rx="18" fill="url(#gold)" stroke="{O}" stroke-width="5"/>')
    g.append(f'<g clip-path="url(#{cid})"><image x="{x}" y="{y}" width="{w}" height="{h}" preserveAspectRatio="xMidYMid slice" xlink:href="{uri(img)}"/>')
    g.append(f'<rect x="{x}" y="{y+h-90}" width="{w}" height="90" fill="url(#band)"/></g>')
    g.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="14" fill="none" stroke="{O}" stroke-width="4"/>')
    g.append(f'<circle cx="{x+30}" cy="{y+30}" r="23" fill="url(#gold)" stroke="{O}" stroke-width="5"/>')
    g.append(f'<text x="{x+30}" y="{y+41}" font-family="Fira Sans" font-weight="900" font-size="30" text-anchor="middle" fill="{O}">{n}</text>')
    g+=title(caption,x+16,y+h-18,27,1,fill="#ffffff",sw=8)
    if badge:
        bw,bh=176,46
        bx,by=x+w-bw+14,y-22
        g.append(f'<rect x="{bx+4}" y="{by+5}" width="{bw}" height="{bh}" rx="23" fill="{O}"/>')
        g.append(f'<rect x="{bx}" y="{by}" width="{bw}" height="{bh}" rx="23" fill="#c0123a" stroke="{O}" stroke-width="5"/>')
        g.append(f'<text x="{bx+bw/2}" y="{by+33}" font-family="Fira Sans" font-weight="900" font-size="27" letter-spacing="2" text-anchor="middle" fill="#fff">{badge}</text>')
    return g

TX=48
s+=title("LAST",TX,170,152,6)
s+=title("STAND",TX,315,152,5)
s+=title("ENDLESS WAVES · ONE ARENA",TX+2,372,34,2,fill="#ffffff",sw=10)
s.append(f'<text x="{TX+2}" y="412" font-family="Fira Sans" font-style="italic" font-weight="800" font-size="30" fill="#ffd24a" stroke="{O}" stroke-width="8" stroke-linejoin="round" paint-order="stroke">How long can you last?</text>')

GX,GY,TW,TH,G=574,34,320,304,22
s+=tile(1,"t1.png",GX,GY,TW,TH,"START IN CAMP")
s+=tile(2,"t2.png",GX+TW+G,GY,TW,TH,"HOLD THE ARENA")
s+=tile(3,"t3.png",GX,GY+TH+G+20,TW,TH,"CLEAR EACH WAVE")
s+=tile(4,"t4.png",GX+TW+G,GY+TH+G+20,TW,TH,"UPGRADE, REPEAT")
s+=tile(5,"gameplay.png",48,452,476,236,"SURVIVE THE HORDE",badge="WAVE 125")
s.append('</svg>')
open(os.path.join(HERE,'preview.svg'),'w').write("\n".join(s))
