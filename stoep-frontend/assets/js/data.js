/* Stoep Barbershop — shared data
   Single source of truth for shop info, services, barbers, hours.
   Booking + calendar generation read from this file so nothing is hard-coded twice. */

const SHOP = {
  name: "Stoep Barbershop",
  legalName: "Stoep Barbershop (Pty) Ltd",
  tagline: "Pull up a chair.",
  addressLine1: "142 Duncan Street",
  addressLine2: "Hatfield, Pretoria, 0028",
  fullAddress: "142 Duncan Street, Hatfield, Pretoria, 0028",
  mapQuery: "142+Duncan+Street+Hatfield+Pretoria",
  phoneDisplay: "012 111 4402",
  phoneHref: "+27121114402",
  email: "hello@stoepbarbers.co.za",
  instagram: "https://instagram.com/stoepbarbers",
  facebook: "https://facebook.com/stoepbarbers",
  founded: 2016,
  // JS getDay(): 0 Sun … 6 Sat. Value = [openMinutes, closeMinutes] or null if closed.
  hoursByDay: {
    0: null,
    1: [510, 1080], // 08:30–18:00
    2: [510, 1080],
    3: [510, 1080],
    4: [510, 1080],
    5: [510, 1080],
    6: [480, 900]   // 08:00–15:00
  },
  hoursDisplay: [
    { label: "Monday – Friday", value: "08:30 – 18:00" },
    { label: "Saturday", value: "08:00 – 15:00" },
    { label: "Sunday", value: "Closed" }
  ]
};

const SERVICES = [
  {
    id: "classic",
    name: "Classic Cut",
    description: "A clean, considered cut with clippers and shears, finished with a straight-razor neckline.",
    price: 220,
    duration: 40
  },
  {
    id: "fade",
    name: "Skin Fade",
    description: "Precision fade blended down to the skin, shaped around your hairline and built up on top to suit you.",
    price: 260,
    duration: 45
  },
  {
    id: "beard",
    name: "Beard Trim & Line-up",
    description: "Beard shaped, edges sharpened, hot towel finish. Ten minutes if you're already growing it out well.",
    price: 150,
    duration: 25
  },
  {
    id: "combo",
    name: "Cut + Beard Combo",
    description: "The Classic Cut or Skin Fade paired with a full beard trim and line-up, done in one sitting.",
    price: 350,
    duration: 60
  },
  {
    id: "kids",
    name: "Kids Cut",
    description: "For customers 12 and under. Same care, shorter chair time, no fuss.",
    price: 150,
    duration: 30
  },
  {
    id: "full",
    name: "The Full Stoep",
    description: "Cut, beard, hot towel and a scalp massage. Book this one when you've got somewhere to be.",
    price: 480,
    duration: 75
  }
];

const BARBERS = [
  {
    id: "neo",
    name: "Neo Mahlangu",
    role: "Owner & Senior Barber",
    specialty: "Skin fades and precision line-ups",
    years: 9,
    photo: "assets/img/barbers/neo.jpg",
    bio: "Neo opened Stoep in 2016 after five years cutting hair in Sunnyside. He trained under a barber who insisted on redoing a fade until the line was right, and that habit stuck. Ask him about the '96 Chiefs squad while he works — he'll have an opinion."
  },
  {
    id: "junior",
    name: "Junior Sithole",
    role: "Barber",
    specialty: "Beard sculpting",
    years: 5,
    photo: "assets/img/barbers/junior.jpg",
    bio: "Junior joined Stoep in 2021 and has quietly become the barber people request by name when they're growing a beard out properly. Patient with awkward in-between stages, and honest when a shape isn't going to work."
  },
  {
    id: "amahle",
    name: "Amahle Dube",
    role: "Barber",
    specialty: "Classic and textured cuts",
    years: 4,
    photo: "assets/img/barbers/amahle.jpg",
    bio: "Amahle trained in Johannesburg before moving to Pretoria in 2022. Known for cuts that still look sharp three weeks later, and for talking you out of a fringe trim you'll regret."
  }
];

const TESTIMONIALS = [
  { quote: "Told Neo I wanted 'shorter but not too short' and somehow he understood exactly what I meant. Still does.", name: "Thabo M." },
  { quote: "First place that's kept my beard the same shape for two years running. I stopped shopping around.", name: "Rian K." },
  { quote: "Took my son for his first proper cut. Junior was patient with a fidgety 6-year-old and it looked great after.", name: "Palesa N." }
];
