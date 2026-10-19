// Synthetic receive fixtures only. No SDR access or transmitter code path.
use num_complex::Complex;
use std::{fs,io::Write};
fn hex(s:&str)->Vec<u8>{s.as_bytes().chunks(2).map(|p|u8::from_str_radix(std::str::from_utf8(p).unwrap(),16).unwrap()).collect()}
fn main(){
 let dir=std::env::args().nth(1).expect("output directory");fs::create_dir_all(&dir).unwrap();let rate=2_400_000.0;
 let spec=xng_mode_acars::modulate::FrameSpec{mode:'2',tail:"N000MR",ack:None,label:"H1",block_id:'3',msg_num:Some("M01A"),flight:Some("MR0001"),text:"MRS SYNTHETIC AVIATION TEST",etb:false};
 write(&dir,"acars",xng_mode_acars::modulate::burst_iq(&spec,rate,250000.0,0.5));
 use xng_mode_vdl2::avlc::{encode_address,AddressType};
 let mut f=Vec::new();f.extend(encode_address(AddressType::GroundIcao,0x123456,false,false));f.extend(encode_address(AddressType::Aircraft,0xABCDEF,false,true));f.push(0x01);
 write(&dir,"vdl2",xng_mode_vdl2::modulate::burst_iq(&[f],rate,250000.0,0.5));
 // Published ADS-B CPR example, repeated to exercise decoder acquisition gates.
 let mut iq=Vec::new();for _ in 0..4{for s in ["8D40621D58C382D690C8AC2863A7","8D40621D58C386435CC412692AD6"]{let pulse=xng_mode_adsb::modulate::frame_iq(&hex(s),10,0.6);for n in 0..((pulse.len() as f64*0.24) as usize){iq.push(pulse[((n as f64+0.5)/0.24) as usize]);}iq.extend(vec![Complex::new(0.0,0.0);2400]);}}
 write(&dir,"adsb",iq);
 let mut payload=hex("00a66ef135445d525a0c0519119021204800");payload[1]=0xAB;payload[2]=0xCD;payload[3]=0xEF;
 let parity=xng_mode_uat::fec::encode_downlink_short(&payload);payload.extend(parity);
 write(&dir,"uat",xng_mode_uat::modulate::burst_iq(&payload,true,rate,100000.0,0.6));
}
fn write(dir:&str,mode:&str,burst:Vec<Complex<f32>>){
 let mut iq=vec![Complex::new(0.0,0.0);24000];iq.extend(burst);iq.extend(vec![Complex::new(0.0,0.0);1_600_000]);
 let mut file=fs::File::create(format!("{dir}/{mode}.u8")).unwrap();for chunk in iq.chunks(8192){let bytes:Vec<u8>=chunk.iter().flat_map(|s|[(s.re*127.0+127.5).clamp(0.0,255.0) as u8,(s.im*127.0+127.5).clamp(0.0,255.0) as u8]).collect();file.write_all(&bytes).unwrap();}
}

