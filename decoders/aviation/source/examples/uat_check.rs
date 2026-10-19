use num_complex::Complex;
fn main(){
 let p=[0x00,0xa6,0x6e,0xf1,0x35,0x44,0x5d,0x52,0x5a,0x0c,0x05,0x19,0x11,0x90,0x21,0x20,0x48,0x00];let mut wire=p.to_vec();wire.extend(xng_mode_uat::fec::encode_downlink_short(&p));
 for rate in [2083334.0,2400000.0,4000000.0]{for chunk in [8192,100000]{let mut d=xng_mode_uat::UatChannelDecoder::new(rate).unwrap();let mut iq=vec![Complex::new(0.0,0.0);24000];iq.extend(xng_mode_uat::modulate::burst_iq(&wire,true,rate,0.0,0.6));iq.extend(vec![Complex::new(0.0,0.0);30000]);let mut n=0;for c in iq.chunks(chunk){n+=d.process(c).len();}println!("rate {rate} chunk {chunk} frames {n}");}}
}
