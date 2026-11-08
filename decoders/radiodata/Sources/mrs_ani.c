/* Receive-only stdin bridge. Decoder copyright/license notices retained in ani/. */
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <io.h>
#include <fcntl.h>
#include "ani/mdc_decode.h"
#include "ani/fsync_decode.h"
static void hex(const unsigned char*p,int n){for(int i=0;i<n;i++)printf("%02X",p[i]);}
static void mdc(int frames,unsigned char op,unsigned char arg,unsigned short unit,unsigned char a,unsigned char b,unsigned char c,unsigned char d,void*ctx){if(frames<2)a=b=c=d=0;printf("{\"protocol\":\"MDC1200\",\"frames\":%d,\"op\":%u,\"arg\":%u,\"unit\":%u,\"extra\":\"%02X%02X%02X%02X\"}\n",frames,op,arg,unit,a,b,c,d);}
static void fs(int cmd,int sub,int ff,int fu,int tf,int tu,int all,unsigned char*payload,int plen,unsigned char*raw,int rlen,void*ctx,int fs2,int fast){if(plen<0||plen>1024||rlen<0||rlen>1024)return;printf("{\"protocol\":\"FleetSync\",\"cmd\":%d,\"subcmd\":%d,\"from_fleet\":%d,\"from_unit\":%d,\"to_fleet\":%d,\"to_unit\":%d,\"all\":%d,\"fsync2\":%d,\"fast\":%d,\"payload\":\"",cmd,sub,ff,fu,tf,tu,all,fs2,fast);hex(payload,plen);printf("\",\"raw\":\"");hex(raw,rlen);printf("\"}\n");}
int main(void){_setmode(_fileno(stdin),_O_BINARY);setvbuf(stdout,NULL,_IONBF,0);mdc_decoder_t*m=mdc_decoder_new(48000);fsync_decoder_t*f=fsync_decoder_new(48000);if(!m||!f)return 2;mdc_decoder_set_callback(m,mdc,NULL);fsync_decoder_set_callback(f,fs,NULL);int16_t pcm[4096];unsigned char samples[4096];size_t n;while((n=fread(pcm,2,4096,stdin))){for(size_t i=0;i<n;i++)samples[i]=(pcm[i]>>8)+128;mdc_decoder_process_samples(m,samples,(int)n);fsync_decoder_process_samples(f,samples,(int)n);}fsync_decoder_end_samples(f);return 0;}
