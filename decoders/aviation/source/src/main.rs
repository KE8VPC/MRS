use std::io::{self, Read, Write};
use num_complex::Complex;
use xng_types::{Provenance, StationIdentity, AppInfo, Message};

fn main() { if let Err(e)=run(){ eprintln!("{e}"); std::process::exit(1); } }
fn run()->Result<(),Box<dyn std::error::Error>> {
    let a:Vec<String>=std::env::args().collect();
    if a.len()!=6 {return Err("Usage: mrs-aviation mode rate offset frequency format(u8|s8)".into());}
    let rate:f64=a[2].parse()?; let offset:f64=a[3].parse()?; let freq:u64=a[4].parse()?;
    if !rate.is_finite()||rate<24000.0||rate>20000000.0||!offset.is_finite()||offset.abs()>rate/2.0 {return Err("Invalid rate/offset".into());}
    let signed=match a[5].as_str(){"u8"=>false,"s8"=>true,_=>return Err("Unsupported sample format".into())};
    let source=Provenance{station:StationIdentity::new("MRS-local"),app:AppInfo{name:"MRS aviation worker".into(),version:"0.1.0".into()},sdr:None,channel:None};
    let mode=a[1].as_str();
    let mut acars=if mode=="acars"{Some(xng_mode_acars::AcarsChannelDecoder::new(rate,offset)?)}else{None};
    let mut vdl=if mode=="vdl2"{Some(xng_mode_vdl2::Vdl2ChannelDecoder::new(rate,offset)?)}else{None};
    let adsb_rate=if rate>4000000.0 {4000000.0} else {rate};
    let mut adsb_ddc=if mode=="adsb" && rate>4000000.0 {Some(xng_dsp::Ddc::new(rate,adsb_rate,offset,1000000.0)?)}else{None};
    let mut adsb_iq=Vec::new();
    let mut adsb=if mode=="adsb"{Some(xng_mode_adsb::AdsbDecoder::new_live(adsb_rate)?)}else{None};
    let mut uat=if mode=="uat"{Some(xng_mode_uat::UatChannelDecoder::new(rate)?)}else{None};
    if acars.is_none()&&vdl.is_none()&&adsb.is_none()&&uat.is_none(){return Err("Unknown mode".into());}
    let mut input=io::stdin().lock(); let mut output=io::BufWriter::new(io::stdout().lock());
    let mut oscillator=Complex::<f32>::new(1.0,0.0);
    let step=Complex::from_polar(1.0,(-2.0*std::f64::consts::PI*offset/rate) as f32);
    loop {
        let mut header=[0u8;4];
        // EOF at a block boundary is normal; partial blocks are errors.
        if input.read(&mut header[..1])?==0 {break;}
        input.read_exact(&mut header[1..])?;
        let count=u32::from_le_bytes(header) as usize;
        if count==0||count>262144||count%2!=0 {return Err("Invalid block length".into());}
        let mut raw=vec![0;count];input.read_exact(&mut raw)?;
        let mut iq:Vec<Complex<f32>>=raw.chunks_exact(2).map(|b|if signed{Complex::new(b[0] as i8 as f32/128.0,b[1] as i8 as f32/128.0)}else{Complex::new((b[0] as f32-127.5)/128.0,(b[1] as f32-127.5)/128.0)}).collect();
        // Remove receiver DC before ADS-B magnitude detection or channel shifting.
        // Otherwise a small capture offset leaves the tuner artifact inside the channel.
        if adsb.is_some() && !iq.is_empty() {let mean=iq.iter().copied().sum::<Complex<f32>>()/iq.len() as f32;for sample in &mut iq{*sample-=mean;}}
        if uat.is_some()&&offset!=0.0 {for s in &mut iq{*s*=oscillator;oscillator*=step;}oscillator/=oscillator.norm();}
        let mut messages:Vec<Message>=Vec::new();
        if let Some(d)=&mut acars{for f in d.process(&iq){messages.push(xng_mode_acars::to_message(&f,freq,d.level_dbfs(),d.noise_dbfs(),source.clone()));}}
        if let Some(d)=&mut vdl{for f in d.process(&iq){let mut m=xng_mode_vdl2::to_message(&f,freq,0.0,source.clone());m.signal.rssi_db=None;messages.push(m);}}
        if let Some(d)=&mut adsb{adsb_iq.clear();let samples=if let Some(front)=&mut adsb_ddc{front.process(&iq,&mut adsb_iq);&adsb_iq}else{&iq};for f in d.process(samples){messages.push(xng_mode_adsb::to_message(&f,freq,source.clone()));}}
        if let Some(d)=&mut uat{for f in d.process(&iq){messages.push(xng_mode_uat::to_message(&f,freq,source.clone()));}}
        for m in messages{serde_json::to_writer(&mut output,&m)?;output.write_all(b"\n")?;}
        output.flush()?;
    }
    Ok(())
}


