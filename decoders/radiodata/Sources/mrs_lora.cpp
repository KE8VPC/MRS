/* Receive-only CF32 stdin wrapper for the GPL LoRa PHY in lora/. */
#include <cstdio>
#include <cstdlib>
#include <io.h>
#include <fcntl.h>
#include "lora/lora_lite.h"
static int sf;static long long samples_fed;
static void received(const lora_lite::Packet&p,void*){std::printf("{\"sample_index\":%lld,\"status\":%u,\"length\":%u,\"has_crc\":%s,\"crc_ok\":%s,\"crc_method\":%u,\"sync\":%u,\"snr_db\":%.3f,\"cfo_bins\":%.3f,\"sf\":%d,\"hex\":\"",samples_fed,p.status,p.hdr_len,p.has_crc?"true":"false",p.crc_ok?"true":"false",p.crc_method,p.sync,p.snr,p.cfo,sf);for(int i=0;i<p.len;i++)std::printf("%02X",p.data[i]);std::puts("\"}");}
int main(int argc,char**argv){if(argc!=7)return 2;sf=std::atoi(argv[1]);int bw=std::atoi(argv[2]),cr=std::atoi(argv[3]),sync=std::atoi(argv[4]);unsigned long freq=std::strtoul(argv[5],nullptr,10);if(sf<7||sf>12||bw<15625||bw>500000||cr<1||cr>4||sync<0||sync>255)return 2;_setmode(_fileno(stdin),_O_BINARY);setvbuf(stdout,nullptr,_IONBF,0);lora_lite::Demod d;d.init(sf,cr,true,sync,received,nullptr,bw,freq);double rate=std::atof(argv[6]),step=rate/(2.0*bw);if(!std::isfinite(rate)||step<1||step>2)return 2;lora_lite::Cx samples[4096],out[8192],previous{0,0};size_t n;double next=0;long long at=0;bool started=false;while((n=fread(samples,sizeof(samples[0]),4096,stdin))){int used=0;for(size_t i=0;i<n;i++,at++){auto current=samples[i];if(started)while(next<=at){double a=next-(at-1);out[used++]={(float)(previous.r+(current.r-previous.r)*a),(float)(previous.i+(current.i-previous.i)*a)};next+=step;if(used==8192){samples_fed+=used;d.feed(out,used);used=0;}}else{started=true;next=1;}previous=current;}if(used){samples_fed+=used;d.feed(out,used);}}return 0;}
