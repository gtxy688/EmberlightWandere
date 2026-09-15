from pathlib import Path
import math, random, struct, wave, json

root = Path(__file__).resolve().parent.parent
out = root/'Assets/Resources/Audio/Sfx/Generated'
rate = 44100
specs = [('fireball',.23,.48),('orbit_ignite',.48,.35),('burn_ground',.62,.25),('pierce_arrow',.19,.40),('boomerang',.52,.34),('meteor',.72,.52)]
reports = []
preview = []
for kind, duration, peak in specs:
    rng = random.Random(kind)
    samples = []
    low = mid = phase = phase2 = 0.0
    pops = [(rng.uniform(.04,duration-.06),rng.uniform(.015,.035)) for _ in range(7)]
    for i in range(round(rate*duration)):
        t=i/rate; u=t/duration
        white=rng.uniform(-1,1)
        low += .025*(white-low)
        mid += .19*(white-mid)
        band=mid-low
        attack=min(1,t/.008)
        tail=min(1,(duration-t)/.04)
        if kind=='fireball':
            freq=110+340*math.exp(-t*24)
            phase+=2*math.pi*freq/rate
            x=(.60*math.sin(phase)*math.exp(-t*24)+1.8*band*math.exp(-t*16))*attack*tail
        elif kind=='orbit_ignite':
            phase+=2*math.pi*(360+200*u)/rate
            phase2+=2*math.pi*740/rate
            x=(.18*math.sin(phase)+.10*math.sin(phase2)+1.8*band)*math.sin(math.pi*u)**1.3*tail
        elif kind=='burn_ground':
            crack=sum(math.exp(-(t-p)/w) for p,w in pops if t>=p)
            x=(2.7*low+.7*band+band*crack*1.9)*math.sin(math.pi*u)**.7*attack*tail
        elif kind=='pierce_arrow':
            phase+=2*math.pi*(1700-1350*u)/rate
            x=(.22*math.sin(phase)*math.exp(-t*38)+2.4*band)*math.sin(math.pi*u)**1.4*tail
        elif kind=='boomerang':
            phase+=2*math.pi*(450+230*math.sin(math.pi*u))/rate
            flutter=.55+.45*math.sin(2*math.pi*13*t)
            x=(1.5*band*flutter+.16*math.sin(phase))*math.sin(math.pi*u)**1.2*tail
        else:
            phase+=2*math.pi*(65+115*math.exp(-t*25))/rate
            x=(.65*math.sin(phase)*math.exp(-t*10)+3.8*low*math.exp(-t*5)+1.2*band*math.exp(-t*18))*attack*tail
        samples.append(x)
    # Remove DC, normalize conservatively, and taper both endpoints.
    mean=sum(samples)/len(samples)
    samples=[x-mean for x in samples]
    gain=peak/max(abs(x) for x in samples)
    samples=[x*gain*min(1,i/220,(len(samples)-1-i)/440) for i,x in enumerate(samples)]
    assert all(math.isfinite(x) and abs(x)<1 for x in samples)
    def save(path,data):
        with wave.open(str(path),'wb') as f:
            f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate)
            f.writeframes(b''.join(struct.pack('<h',round(x*32767)) for x in data))
    save(out/('weapon_'+kind+'.wav'),samples)
    reports.append(dict(name=kind,seconds=duration,peak=round(max(map(abs,samples)),3),rms=round(math.sqrt(sum(x*x for x in samples)/len(samples)),3)))
    preview.extend(samples+[0.0]*int(rate*.45))
save(root/'weapon-preview.wav',preview)
(root/'audio-checks.json').write_text(json.dumps(reports,indent=2))
print(json.dumps(reports))
