import wave, struct, math, random

SR = 44100
DUR = 21.2
N = int(SR*DUR)
random.seed(7)

L = [0.0]*N
R = [0.0]*N

def add(t0, dur, fn, panL=1.0, panR=1.0):
    s = int(t0*SR); e = min(N, int((t0+dur)*SR))
    for i in range(s, e):
        tt = (i-s)/SR
        v = fn(tt)
        L[i]+=v*panL; R[i]+=v*panR

def adsr(t, dur, a, d, s, r, peak=1.0, sus=0.7):
    if t<a: return peak*(t/a)
    if t<a+d: return peak - (peak-peak*sus)*((t-a)/d)
    if t<dur-r: return peak*sus
    if t<dur: return peak*sus*(1-(t-(dur-r))/r)
    return 0.0

def pad_voice(freq):
    def f(t):
        env = adsr(t, CH, 0.35, 0.4, 0.7, 0.5, peak=1.0, sus=0.72)
        # soft timbre: fundamental + gentle harmonics
        x = math.sin(2*math.pi*freq*t)
        x += 0.42*math.sin(2*math.pi*2*freq*t)
        x += 0.18*math.sin(2*math.pi*3*freq*t)
        # slow vibrato shimmer
        x *= 1.0+0.015*math.sin(2*math.pi*5.0*t)
        return env*x
    return f

def bass_voice(freq):
    def f(t):
        env = adsr(t, CH, 0.06, 0.3, 0.6, 0.4, peak=1.0, sus=0.7)
        x = math.sin(2*math.pi*freq*t) + 0.25*math.sin(2*math.pi*2*freq*t)
        return env*x
    return f

# --- chord progression (C major, uplifting): C G Am F, x2 over 20s ---
CH = 2.65
prog = [
    ([261.63,329.63,392.00], 130.81),  # C
    ([196.00,246.94,293.66], 98.00),   # G
    ([220.00,261.63,329.63], 110.00),  # Am
    ([174.61,220.00,261.63], 87.31),   # F
]
PADV=0.15; BASSV=0.14
for loop in range(2):
    for ci,(notes,root) in enumerate(prog):
        t0 = (loop*4+ci)*CH
        pan = 0.9+0.1*math.sin(ci)
        for ni,nf in enumerate(notes):
            add(t0, CH, lambda t,fn=pad_voice(nf): PADV*fn(t),
                panL=1.0-0.12*ni, panR=0.88+0.12*ni)
        add(t0, CH, lambda t,fn=bass_voice(root): BASSV*fn(t))

# --- gentle kick, four-on-floor at 100bpm, soft & playful ---
beat=0.6
def kick(t):
    f = 120*math.exp(-t*22)+45
    env = math.exp(-t*9)
    return env*math.sin(2*math.pi*f*t)
tb=0.0
# start kicks after the hook settles (~2.4s), duck out during outro swell start
while tb < DUR-0.2:
    if tb>2.3:
        vol = 0.5
        if tb>17.0: vol=0.32
        add(tb, 0.32, lambda t,v=vol: v*kick(t))
    tb+=beat

# --- shaker (filtered noise) on offbeats for movement ---
def shaker(t):
    env = math.exp(-t*45)
    return env*(random.random()*2-1)
tb=beat/2
while tb < DUR-0.3:
    if tb>3.2 and tb<17.2:
        add(tb, 0.12, lambda t: 0.05*shaker(t), panL=0.8,panR=1.0)
    tb+=beat

# --- blips (bell sine) at scene changes + key beats ---
def blip(freq):
    def f(t):
        env=math.exp(-t*7)
        return env*(math.sin(2*math.pi*freq*t)+0.5*math.sin(2*math.pi*2*freq*t))
    return f
# scene transitions
for tt,fr in [(3.4,659.25),(9.6,783.99),(13.4,659.25),(17.0,987.77)]:
    add(tt,0.7, lambda t,fn=blip(fr): 0.13*fn(t), panL=0.95,panR=1.0)
# auto-plan: rising dings as slots fill (S2)
for tt,fr in [(5.9,523.25),(6.2,587.33),(6.5,659.25),(7.05,783.99)]:
    add(tt,0.45, lambda t,fn=blip(fr): 0.10*fn(t), panL=1.0,panR=0.95)
# planning-klaar accent
add(7.15,0.8, lambda t,fn=blip(1046.50): 0.13*fn(t))
# badge confirms in S3 (rising)
for tt,fr in [(11.3,659.25),(11.6,783.99),(11.9,1046.50)]:
    add(tt,0.45, lambda t,fn=blip(fr): 0.09*fn(t), panL=1.0,panR=0.95)
# payment confirm accent (S4 -> ok at ~16.1)
add(16.1,0.8, lambda t,fn=blip(1046.50): 0.14*fn(t))

# --- outro chord swell (C major, bright) ---
def swell(freq):
    def f(t):
        env = min(1.0, t/0.8)*math.exp(-max(0,t-2.0)*1.2)
        x = math.sin(2*math.pi*freq*t)+0.4*math.sin(2*math.pi*2*freq*t)+0.2*math.sin(2*math.pi*3*freq*t)
        return env*x
    return f
for nf in [261.63,329.63,392.00,523.25]:
    add(17.0, DUR-17.0, lambda t,fn=swell(nf): 0.055*fn(t))

# --- master: gentle soft-clip + fade in/out, normalize ---
peak=max(1e-6, max(max(abs(x) for x in L), max(abs(x) for x in R)))
g=0.82/peak
def softclip(x):
    return math.tanh(x*1.1)/1.1
fw=wave.open("track.wav","w")
fw.setnchannels(2); fw.setsampwidth(2); fw.setframerate(SR)
fade=int(0.05*SR); fo=int(0.6*SR)
frames=bytearray()
for i in range(N):
    a=1.0
    if i<fade: a=i/fade
    if i>N-fo: a=(N-i)/fo
    l=softclip(L[i]*g)*a; r=softclip(R[i]*g)*a
    frames+=struct.pack('<hh', int(max(-1,min(1,l))*32767), int(max(-1,min(1,r))*32767))
fw.writeframes(bytes(frames)); fw.close()
print("track.wav written", round(DUR,1),"s")
