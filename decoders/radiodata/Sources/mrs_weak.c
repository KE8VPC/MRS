/* Receive-only framed float audio bridge; upstream notices under ft8/ and wspr/. */
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <math.h>
#include <io.h>
#include <fcntl.h>
#include "ft8/ft8/decode.h"
#include "ft8/ft8/message.h"
#include "ft8/ft8/encode.h"
#include "ft8/common/monitor.h"
#include "wspr/wsprd/wsprd.h"
#include "wspr/wsprd/wsprsim_utils.h"
static void text(const char*s){putchar('"');for(;*s;s++){unsigned char c=*s;if(c=='"'||c=='\\')putchar('\\');if(c>=32)putchar(c);}putchar('"');}
static double padding;
/* Estimate signal/noise power with decoded-tone matched filters. Rectangular
   symbol filters have ENBW Fs/N; normalize the result to 2500 Hz noise BW.
   Off-signal bins use an exponential-noise median / ln(2), reducing interference
   bias. This is an estimate, not the decoder's synchronization score. */
static double tone_power(const float*s,int n,int at,int size,double hz){
 if(at<0||at+size>n||hz<100||hz>4400)return -1;
 double coeff=2*cos(2*M_PI*hz/12000),a=0,b=0;
 for(int i=0;i<size;i++){double next=s[at+i]+coeff*a-b;b=a;a=next;}
 double power=a*a+b*b-coeff*a*b;return fmax(0,power)/((double)size*size);
}
static int cmp_double(const void*a,const void*b){double x=*(const double*)a,y=*(const double*)b;return (x>y)-(x<y);}
static double estimate_snr(const float*s,int n,const ftx_message_t*msg,int ft4,double start,double base){
 uint8_t tones[105];if(ft4)ft4_encode(msg->payload,tones);else ft8_encode(msg->payload,tones);
 int size=ft4?576:1920,total=ft4?105:79,offset=(int)llround(start*12000),best=offset,used=0;double signal=-1,noise[400];int count=0;double spacing=12000.0/size;
 for(int shift=-8;shift<=8;shift++){int at=offset+shift*size/4;double sum=0;int valid=0;
  for(int k=2;k<total-2;k+=3){double v=tone_power(s,n,at+k*size,size,base+tones[k]*spacing);if(v>=0){sum+=v;valid++;}}
  if(valid>10&&sum/valid>signal){signal=sum/valid;best=at;used=valid;}}
 if(used<10)return NAN;
 for(int k=2;k<total-2;k+=3)for(int j=0;j<8;j++){int bin=j<4?-24+j*4:(ft4?4:8)+12+(j-4)*4;double v=tone_power(s,n,best+k*size,size,base+bin*spacing);if(v>=0&&count<400)noise[count++]=v;}
 if(count<30)return NAN;qsort(noise,count,sizeof(double),cmp_double);double floor=noise[count/2]/log(2.0);
 if(floor<1e-20||signal<=floor)return NAN;return fmax(-50,fmin(50,10*log10((signal-floor)/floor*spacing/2500.0)));
}
static void ft(const float*s,int n,int ft4){monitor_t m;monitor_config_t cfg={.f_min=100,.f_max=4000,.sample_rate=12000,.time_osr=2,.freq_osr=2,.protocol=ft4?FTX_PROTOCOL_FT4:FTX_PROTOCOL_FT8};monitor_init(&m,&cfg);int blocks=(n+m.block_size-1)/m.block_size;if(blocks>m.wf.max_blocks){void*memory=realloc(m.wf.mag,(size_t)blocks*m.wf.block_stride*sizeof(*m.wf.mag));if(!memory)exit(2);m.wf.mag=memory;m.wf.max_blocks=blocks;}for(int i=0;i+m.block_size<=n;i+=m.block_size)monitor_process(&m,s+i);ftx_candidate_t cand[160];int count=ftx_find_candidates(&m.wf,160,cand,10);ftx_message_t seen[100];int used=0;for(int i=0;i<count&&used<100;i++){ftx_message_t msg;ftx_decode_status_t status;if(!ftx_decode_candidate(&m.wf,cand+i,30,&msg,&status))continue;int duplicate=0;for(int j=0;j<used;j++)if(!memcmp(seen[j].payload,msg.payload,sizeof msg.payload))duplicate=1;if(duplicate)continue;seen[used++]=msg;char decoded[FTX_MAX_MESSAGE_LENGTH];ftx_message_offsets_t offsets;if(ftx_message_decode(&msg,NULL,decoded,&offsets)!=FTX_MESSAGE_RC_OK)continue;double freq=(m.min_bin+cand[i].freq_offset+(float)cand[i].freq_sub/m.wf.freq_osr)/m.symbol_period;double dt=(cand[i].time_offset+(float)cand[i].time_sub/m.wf.time_osr)*m.symbol_period;dt-=padding;double dt_error=dt-(ft4?.5:.66)-128.0/48000.0;printf("{\"protocol\":\"%s\",\"audio_hz\":%.3f,\"dt\":%.3f,\"dt_error\":%.6f,\"sync_score\":%d,\"text\":",ft4?"FT4":"FT8",freq,dt,dt_error,cand[i].score);text(decoded);double snr=estimate_snr(s,n,&msg,ft4,dt+padding,freq);if(isfinite(snr))printf(",\"snr_db\":%.2f,\"snr_method\":\"decoded-tone power / 2500 Hz noise estimate\"",snr);else printf(",\"snr_db\":null");printf("}\n");}monitor_free(&m);}
static void ws(const float*s,int n){int count=n/32;float*i=calloc(count,sizeof(float)),*q=calloc(count,sizeof(float));if(!i||!q)exit(2);/* Mix the 1500-Hz USB center, FIR low-pass and decimate to 375 complex samples/s. */
const int taps=257;double h[taps],sum=0;for(int k=0;k<taps;k++){double x=k-(taps-1)/2.;h[k]=(x==0?2*160./12000:sin(2*M_PI*160*x/12000)/(M_PI*x))*(.54-.46*cos(2*M_PI*k/(taps-1)));sum+=h[k];}for(int t=0;t<count;t++){int at=t*32;double re=0,im=0;for(int k=0;k<taps;k++){int j=at-k;if(j<0)break;double phase=2*M_PI*1500*j/12000;re+=s[j]*cos(phase)*h[k]/sum;im-=s[j]*sin(phase)*h[k]/sum;}i[t]=re*2;q[t]=im*2;}struct decoder_options o={0};o.npasses=2;o.subtraction=1;struct decoder_results results[MAX_UNIQUES];int found=0;wspr_decode(i,q,count,o,results,&found);for(int t=0;t<found&&t<MAX_UNIQUES;t++){printf("{\"protocol\":\"WSPR\",\"audio_hz\":%.3f,\"dt\":%.3f,\"snr_db\":%.2f,\"drift\":%.3f,\"text\":",results[t].freq*1e6,results[t].dt,results[t].snr,results[t].drift);text(results[t].message);printf(",\"call\":");text(results[t].call);printf(",\"grid\":");text(results[t].loc);printf(",\"power_dbm\":");text(results[t].pwr);printf("}\n");}free(i);free(q);}

/* Separate bounded encoder interface. No device or network access. */
static int encode_tones(int argc,char**argv){
 if(argc!=4||strlen(argv[3])>35)return 2;uint8_t tones[162];int n=0;
 if(!strcmp(argv[2],"FT8")||!strcmp(argv[2],"FT4")){
  ftx_message_t m;ftx_message_init(&m);if(ftx_message_encode(&m,NULL,argv[3])!=FTX_MESSAGE_RC_OK||ftx_message_get_type(&m)!=FTX_MESSAGE_TYPE_STANDARD)return 4;
  if(!strcmp(argv[2],"FT8")){ft8_encode(m.payload,tones);n=79;}else{ft4_encode(m.payload,tones);n=105;}
 }else if(!strcmp(argv[2],"WSPR")){
  char call[7],grid[5],tail[2];int power;char copy[36];
  if(sscanf(argv[3],"%6s %4s %d %1s",call,grid,&power,tail)!=3||strlen(call)<4||strlen(call)>6||strlen(grid)!=4||grid[0]<'A'||grid[0]>'R'||grid[1]<'A'||grid[1]>'R'||grid[2]<'0'||grid[2]>'9'||grid[3]<'0'||grid[3]>'9'||power<0||power>60)return 4;
  if(pack_basecall(call,strlen(call))<0)return 4;
  char*h=calloc(32768,13),*l=calloc(32768,7);if(!h||!l)return 2;strcpy(copy,argv[3]);int ok=get_wspr_channel_symbols(copy,h,l,tones);free(h);free(l);if(!ok)return 4;n=162;
 }else return 2;
 for(int i=0;i<n;i++)printf("%s%d",i?",":"",tones[i]);puts("");return 0;
}

int main(int argc,char**argv){if(argc>1&&!strcmp(argv[1],"--tones"))return encode_tones(argc,argv);if(argc!=2&&argc!=3)return 2;padding=argc==3?atof(argv[2]):0;if(!isfinite(padding)||padding<0||padding>1)return 2;int mode=!strcmp(argv[1],"FT8")?0:!strcmp(argv[1],"FT4")?1:!strcmp(argv[1],"WSPR")?2:-1;if(mode<0)return 2;_setmode(_fileno(stdin),_O_BINARY);setvbuf(stdout,NULL,_IONBF,0);uint32_t n;while(fread(&n,4,1,stdin)==1){if(n<12000||n>1440000)return 3;float*s=malloc(n*sizeof(float));if(!s)return 2;if(fread(s,4,n,stdin)!=n){free(s);return 3;}for(uint32_t j=0;j<n;j++)if(!isfinite(s[j]))s[j]=0;if(mode==2)ws(s,n);else ft(s,n,mode);free(s);puts("{\"done\":true}");}return 0;}
