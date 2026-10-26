/* M.R.S. receive-only I/Q channel bridge. Original implementation, 2026.
   stdin: raw interleaved u8 or s8; stdout: raw cf32 IQ. No SDR/network/TX API.
   Integer decimation, windowed-sinc anti-alias FIR and phase-continuous mixer. */
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>
#include <fcntl.h>
#include <io.h>
#define PI 3.14159265358979323846
int main(int argc,char **argv){
 if(argc!=6)return 2;
 int rate=atoi(argv[1]),factor=atoi(argv[2]),sign=atoi(argv[4]);double offset=atof(argv[3]);
 if(rate<240000||rate>20000000||factor<1||factor>128||!isfinite(offset)||fabs(offset)>rate/2.0)return 2;
 _setmode(_fileno(stdin),_O_BINARY);_setmode(_fileno(stdout),_O_BINARY);
 setvbuf(stdout,NULL,_IOFBF,65536);
 int taps=factor==1?1:factor*24+1,pos=0,phase=0;double *h=calloc(taps,sizeof(double)),*ri=calloc(taps,sizeof(double)),*rq=calloc(taps,sizeof(double));
 if(!h||!ri||!rq)return 3;
 double requested=atof(argv[5]);if(!isfinite(requested)||requested<0||requested>rate/(double)factor*.45)return 2;
 double sum=0,cut=requested>0?requested/rate:.40/factor;
 for(int j=0;j<taps;j++){double m=j-(taps-1)/2.0;h[j]=taps==1?1:(m==0?2*cut:sin(2*PI*cut*m)/(PI*m))*(.42-.5*cos(2*PI*j/(taps-1))+.08*cos(4*PI*j/(taps-1)));sum+=h[j];}
 for(int j=0;j<taps;j++)h[j]/=sum;
 double angle=-2*PI*offset/rate,cs=cos(angle),sn=sin(angle),oc=1,os=0;unsigned char in[65536];float out[16384];size_t n;int used=0,renorm=0;
 while((n=fread(in,1,sizeof(in),stdin))>0){if(n%2)return 4;
  for(size_t j=0;j<n;j+=2){double i=sign?(int8_t)in[j]/128.0:(in[j]-127.5)/128.0,q=sign?(int8_t)in[j+1]/128.0:(in[j+1]-127.5)/128.0;
   ri[pos]=i*oc-q*os;rq[pos]=i*os+q*oc;double nc=oc*cs-os*sn;os=os*cs+oc*sn;oc=nc;if(++renorm==4096){double norm=hypot(oc,os);oc/=norm;os/=norm;renorm=0;}
   if(++phase==factor){phase=0;double oi=0,oq=0;int k=pos;for(int t=0;t<taps;t++){oi+=h[t]*ri[k];oq+=h[t]*rq[k];if(--k<0)k=taps-1;}
    out[used++]=(float)oi;out[used++]=(float)oq;
    if(used==16384){if(fwrite(out,sizeof(float),used,stdout)!=(size_t)used)return 5;used=0;}}
   if(++pos==taps)pos=0;
  }
  if(used){if(fwrite(out,sizeof(float),used,stdout)!=(size_t)used)return 5;used=0;}if(fflush(stdout))return 5;
 }
 free(h);free(ri);free(rq);return ferror(stdin)?4:0;
}
