// MRS bounded IMBE/FEC bridge. Upstream implementation notices are in Sources/dsd and Sources/mbelib.
#include <cstdlib>
#include <cstring>
#include <cmath>
#include <cstdio>
extern "C" {
#include "mbelib.h"
#include "p25p1_check_hdu.h"
#include "p25p1_check_ldu.h"
}
#define _MAIN
#include "p25p1_const.h"
#define API extern "C" __declspec(dllexport)
struct Voice {mbe_parms cur,prev,enh;};
API void* mrs_create(){Voice* v=new Voice();mbe_initMbeParms(&v->cur,&v->prev,&v->enh);return v;}
API void mrs_free(void* v){delete (Voice*)v;}
API int mrs_imbe(void* handle,const unsigned char* dibits,float* audio){
 if(!handle||!dibits||!audio)return -1;char frame[8][23]={},data[88]={},error[128]={};int e1=0,e2=0;
 for(int j=0;j<72;j++){if(dibits[j]>3)return -1;frame[iW[j]][iX[j]]=dibits[j]>>1;frame[iY[j]][iZ[j]]=dibits[j]&1;}
 Voice* v=(Voice*)handle;mbe_processImbe7200x4400Framef(audio,&e1,&e2,error,frame,data,&v->cur,&v->prev,&v->enh,3);
 for(int n=0;n<160;n++)if(!std::isfinite(audio[n]))audio[n]=0;
 return e2;
}
static void pack(char* data,int words,unsigned char* out){memset(out,0,words*6/8);for(int n=0;n<words*6;n++){int w=words-1-n/6;out[n/8]|=(data[w*6+n%6]&1)<<(7-n%8);}}
API int mrs_metadata(int duid,const unsigned char* dibits,int length,unsigned char* out){
 if(!dibits||!out)return -1;int words=duid==0?20:duid==10?16:12;int parityWords=duid==0?16:24-words;
 if(length!=(duid==0?324:120))return -1;
 char data[120]={},parity[96]={};int at=0;
 for(int group=0;group<2;group++)for(int w=(group?parityWords:words)-1;w>=0;w--){char hex[6]={},par[12]={};int pbits=duid==0?12:4;
  for(int b=0;b<6+pbits;b+=2){unsigned char d=dibits[at++];if(d>3)return -1;char* target=b<6?hex:par;int index=b<6?b:b-6;target[index]=d>>1;target[index+1]=d&1;}
  if(duid==0){int fixes=0;if(check_and_fix_golay_24_6(hex,par,&fixes))return -1;}
  else if(check_and_fix_hamming_10_6_3(hex,par)==2)return -1;
  memcpy((group?parity:data)+w*6,hex,6);
 }
 int bad=duid==0?check_and_fix_redsolomon_36_20_17(data,parity):duid==10?check_and_fix_reedsolomon_24_16_9(data,parity):check_and_fix_reedsolomon_24_12_13(data,parity);
 if(bad)return -1;pack(data,words,out);return words*6/8;
}
// Synthetic fixture encoder: FEC output only, no hardware access.
API int mrs_encode_metadata(int duid,const unsigned char* input,unsigned char* out){
 int words=duid==0?20:duid==10?16:12, pw=duid==0?16:24-words;char data[120]={},parity[96]={};
 for(int n=0;n<words*6;n++)data[(words-1-n/6)*6+n%6]=(input[n/8]>>(7-n%8))&1;
 if(duid==0)encode_reedsolomon_36_20_17(data,parity);else if(duid==10)encode_reedsolomon_24_16_9(data,parity);else encode_reedsolomon_24_12_13(data,parity);
 int at=0;for(int group=0;group<2;group++)for(int w=(group?pw:words)-1;w>=0;w--){char* h=(group?parity:data)+w*6;char p[12]={};if(duid==0)encode_golay_24_6(h,p);else encode_hamming_10_6_3(h,p);for(int b=0;b<6;b+=2)out[at++]=(h[b]<<1)|h[b+1];for(int b=0;b<(duid==0?12:4);b+=2)out[at++]=(p[b]<<1)|p[b+1];}return at;
}
