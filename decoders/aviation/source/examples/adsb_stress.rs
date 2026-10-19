use num_complex::Complex;
use std::{fs::File,io::Write};
fn main(){
 let path=std::env::args().nth(1).unwrap();let rate=19_200_000.0;let mut out=std::io::BufWriter::new(File::create(path).unwrap());
 let mut burst=Vec::new();for _ in 0..4{for hex in ["8D40621D58C382D690C8AC2863A7","8D40621D58C386435CC412692AD6"]{let bytes:Vec<u8>=hex.as_bytes().chunks(2).map(|p|u8::from_str_radix(std::str::from_utf8(p).unwrap(),16).unwrap()).collect();let p=xng_mode_adsb::modulate::frame_iq(&bytes,20,0.65);for n in 0..((p.len() as f64*0.96) as usize){burst.push(p[((n as f64+0.5)/0.96) as usize]);}burst.extend(vec![Complex::new(0.0,0.0);19200]);}}
 let mut rng=123456789u32;let mut osc=Complex::new(1.0f32,0.0);let step=Complex::from_polar(1.0,(-2.0*std::f64::consts::PI*750000.0/rate) as f32);
 for n in 0..57_600_000usize{let mut noise=||{rng^=rng<<13;rng^=rng>>17;rng^=rng<<5;((rng&65535) as f32/65535.0-0.5)*0.08};let mut s=Complex::new(noise()-0.025,noise()-0.025);if n>=9_600_000&&n-9_600_000<burst.len(){s+=burst[n-9_600_000]*osc;}osc*=step;if n%8192==0{osc/=osc.norm();}out.write_all(&[(s.re*127.0+127.5).clamp(0.0,255.0) as u8,(s.im*127.0+127.5).clamp(0.0,255.0) as u8]).unwrap();}
}

